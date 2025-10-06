using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Fusion;
using MyBox;
using PTTI_Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// --------------------------------------------------------------
// FusionVideoSync
// --------------------------------------------------------------
// Purpose: Keeps a Unity VideoPlayer synchronized across clients
// in a Photon Fusion session. The Teacher (StateAuthority in your
// app) controls play/pause/seek/speed and Students mirror the state.
//
// Notes about this edit:
// - No original code lines were removed; only comments were added.
// - Method/field summaries were added to improve readability.
// - Inline comments clarify why certain checks exist (e.g., snap
//   throttling, drift correction, authority guarding).
// - Consider restricting RPC senders to StateAuthority for security
//   (left unchanged to respect your current flow; see RPC region).
// --------------------------------------------------------------

[RequireComponent(typeof(VideoPlayer))]
public class FusionVideoSync : NetworkBehaviour
{
    /// <summary>
    /// Simple action enum used to drive tween feedback on both Teacher and Students.
    /// </summary>
    public enum VideoActionType { Play, Pause, FastForward, FastBackward }

    [Header("Panel Refs")]
    [SerializeField] GameObject videoMenuPanel; // Panel with video controls
    [SerializeField] GameObject videoPlayerPanel; // Panel with VideoPlayer
    [SerializeField] VideoList videoListSO; // ScriptableObject with VideoClip list
    [SerializeField] VideoSelectView videoSelectViewPrefab; // Prefab for video selection buttons
    [SerializeField] List<VideoSelectView> videoSelectViews = new(); // Instantiated buttons
    [SerializeField] ScrollRect videoSelectScrollRect; // ScrollRect containing buttons
    [SerializeField] GameObject bufferLoadingMsg; // "Loading..." message

    [Header("Refs")] public VideoPlayer video; // Assigned VideoPlayer in scene

    [Header("Tuning")]
    [Tooltip("If client time differs by more than this, we snap to the authoritative time.")]
    public double hardSnapThreshold = 0.150; // Hard resync cutoff (seconds)
    [Tooltip("If diff is below hardSnapThreshold, we do a light correction.")]
    public double softCorrectionThreshold = 0.030; // Soft speed correction cutoff (seconds)
    [Tooltip("Max rate change used for tiny drift correction (e.g., 1.02x). Set 0 to disable.")]
    [Range(0f, 0.1f)] public float maxRateDrift = 0.02f; // Caps temporary speed offset used to chase sync

    [Header("Controls")]
    [Tooltip("Seconds to skip when fast-forwarding or rewinding.")]
    public double skipStepSeconds = 10.0; // Seek step for FF/RW

    // --- Networked state replicated to clients ---
    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public bool Playing { get; set; } // Whether authoritative state is playing

    [Networked, OnChangedRender(nameof(OnSeekChanged))]
    public double BaseVideoTime { get; set; } // Authority's video time at BaseServerTime

    [Networked]
    public double BaseServerTime { get; set; } // Authority server time when BaseVideoTime was sampled

    [Networked]
    public float PlaybackSpeed { get; set; } // Authoritative playback speed (1 = normal)

    [Networked, OnChangedRender(nameof(OnClipChanged))]
    public NetworkString<_256> ClipUrl { get; set; } // If UseUrl = true, use this URL for VideoPlayer

    [Networked, OnChangedRender(nameof(OnClipChanged))]
    public NetworkBool UseUrl { get; set; } // Toggle to switch between URL and assigned VideoClip

    [Networked, OnChangedRender(nameof(OnVolumeChanged))]
    public float Volume { get; set; }  // 0..1

    [Networked, OnChangedRender(nameof(OnMuteChanged))]
    public NetworkBool Muted { get; set; }

    // Add with the other Networked fields
    [Networked, OnChangedRender(nameof(OnSelectedIndexChanged))]
    public int SelectedIndex { get; set; }  // -1 = none

    [Header("UI")]
    [SerializeField] Image playImg;               // Play/Pause button icon
    [SerializeField] Sprite[] playSprites;        // [0] = play, [1] = pause
    [SerializeField] Image muteImg;               // Mute/Unmute button icon
    [SerializeField] Sprite[] muteSprites;        // [0] = unmute, [1] = mute
    [SerializeField] Slider videoSlider;          // Progress bar (0..1)
    [SerializeField] Slider volumeSlider;          // Progress bar (0..1)
    [SerializeField] TextMeshProUGUI videoRemainingTimer; // Displays elapsed/total like YouTube
    [SerializeField] RectTransform playAnim, pauseAnim, fastForwardAnim, fastBackwardAnim;
    [SerializeField] TextMeshProUGUI fastForwardTxt;   // Shows FF step seconds
    [SerializeField] TextMeshProUGUI fastBackwardTxt;  // Shows RW step seconds
    [SerializeField] float animationDuration = 0.75f;  // Duration for tween feedback

    private bool _isPrepared;              // Tracks when VideoPlayer.prepareCompleted fired
    private Coroutine _prepareRoutine;     // Current preparation coroutine (authority or client)
    private double lastSnapTime = -999;    // Throttle for hard snaps (avoid snapping every frame)
    private float tempVolume;
    private bool _isDraggingSlider = false;
    private bool _lockSliderUntilSeek = false;
    private double _pendingSeekTime = -1;

    #region Unity
    void Awake()
    {
        // Hide all feedback glyphs at boot. If any are unassigned in Inspector, this would NRE —
        // ensure they are wired in scenes that use the feedback.
        playAnim.localScale = pauseAnim.localScale = fastForwardAnim.localScale = fastBackwardAnim.localScale = Vector3.zero;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            videoMenuPanel.SetActive(true);
            videoPlayerPanel.SetActive(false);

            ListVideo();
        }
        else
        {
            videoMenuPanel.SetActive(false);
            videoPlayerPanel.SetActive(true);
        }

        // Ensure we have a VideoPlayer reference.
        if (!video) video = GetComponent<VideoPlayer>();

        // Subscribe to VideoPlayer events for prepare flow and diagnostics.
        video.errorReceived += OnVideoError;
        video.prepareCompleted += OnVideoPrepared;
        video.loopPointReached += OnVideoEndReached;

        if (Object.HasStateAuthority)
        {
            Volume = 1f; // default full volume
            Muted = false;
            ApplyAudioLocally();
            // Teacher initializes baseline speed and establishes a reference.
            PlaybackSpeed = 1f;
            SetReferenceNow(video.time, Runner.SimulationTime, Playing, PlaybackSpeed);

            // Teacher can interact with the slider to seek.
            if (videoSlider)
            {
                videoSlider.enabled = true;
                videoSlider.onValueChanged.AddListener(OnSliderChanged);

                // Listen for drag start and end
                var sliderEvents = videoSlider.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (!sliderEvents)
                    sliderEvents = videoSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

                var beginDrag = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.BeginDrag
                };
                beginDrag.callback.AddListener((_) => { _isDraggingSlider = true; });
                sliderEvents.triggers.Add(beginDrag);

                var endDrag = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.EndDrag
                };
                endDrag.callback.AddListener((_) =>
                {
                    _isDraggingSlider = false;

                    double targetTime = videoSlider.value * video.length;
                    _pendingSeekTime = targetTime;
                    _lockSliderUntilSeek = true;   // freeze slider at user-chosen value

                    TeacherSeek(targetTime, autoPlay: Playing);
                });
                sliderEvents.triggers.Add(endDrag);
            }

            if (volumeSlider)
            {
                volumeSlider.enabled = true;
                volumeSlider.value = Volume;
                volumeSlider.onValueChanged.AddListener(TeacherSetVolume);
            }

            // 🔑 NEW: Set default index = 0 when Teacher spawns
            if (SelectedIndex < 0 && videoListSO != null && videoListSO.videos.Length > 0)
            {
                SelectedIndex = 0;
                UseUrl = videoListSO.videos[0].useURL;
                ClipUrl = UseUrl ? videoListSO.videos[0].videoURL : default;

                video.source = UseUrl ? VideoSource.Url : VideoSource.VideoClip;
                if (UseUrl)
                    video.url = videoListSO.videos[0].videoURL;
                else
                    video.clip = videoListSO.videos[0].videoClip;

                video.time = 0;
                video.playbackSpeed = 1f;
                video.Pause();

                playImg.sprite = playSprites[0];

                // Broadcast so Students immediately sync to index 0
                StartPrepareAndBroadcast(false, 0);
            }
        }
        else
        {
            // Students prepare and apply whatever state the network indicates.
            ApplyNetworkStateImmediate();

            if (videoSlider && FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher)
            {
                videoSlider.enabled = false; // Students cannot scrub/seek
            }

            if (volumeSlider && FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher)
            {
                volumeSlider.enabled = false;
            }
        }

        // Prepare clip at spawn if assigned; this speeds up first play on all peers.
        if (video.clip != null) video.Prepare();
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid dangling event handlers on domain reloads/scene unloads.
        if (video != null)
        {
            video.errorReceived -= OnVideoError;
            video.prepareCompleted -= OnVideoPrepared;
            video.loopPointReached -= OnVideoEndReached;
        }

        if (videoSlider)
        {
            videoSlider.onValueChanged.RemoveListener(OnSliderChanged);
            volumeSlider.onValueChanged.RemoveListener(TeacherSetVolume);
        }
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid) return; // Network object sanity guard

        if (!Object.HasStateAuthority) StepClientSync();

        // UI sync
        if (video.isPrepared && video.length > 0 && !_isDraggingSlider)
        {
            if (_lockSliderUntilSeek)
            {
                // Keep showing the user’s chosen time until the seek completes
                videoSlider.SetValueWithoutNotify((float)(_pendingSeekTime / video.length));

                // When VideoPlayer actually reaches close to target → unlock
                if (System.Math.Abs(video.time - _pendingSeekTime) < 0.5)
                {
                    _lockSliderUntilSeek = false;
                    _pendingSeekTime = -1;
                }
            }
            else
            {
                // Normal auto-follow
                videoSlider.SetValueWithoutNotify((float)(video.time / video.length));

                if (videoRemainingTimer != null)
                {
                    double elapsed = video.time, total = video.length;
                    videoRemainingTimer.text =
                        $"{(int)(elapsed / 60):00}:{(int)(elapsed % 60):00} / {(int)(total / 60):00}:{(int)(total % 60):00}";
                }
            }
        }
    }
    #endregion

    #region Slider Handling
    /// <summary>
    /// Called when Teacher drags the progress slider; seeks to the corresponding time.
    /// Students have interactable=false so this won't run for them.
    /// </summary>
    private void OnSliderChanged(float value)
    {
        if (!video.isPrepared || video.length <= 0) return; // Ignore until clip is ready

        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher) return; // Authority only

        if (_isDraggingSlider)
        {
            // Optional: show a preview time label like YouTube while dragging
            return;
        }

        double targetTime = value * video.length;
        TeacherSeek(targetTime, autoPlay: Playing);
    }
    #endregion

    #region Public API
    /// <summary>
    /// Teacher UI button to toggle play/pause. Students cannot toggle.
    /// </summary>
    public void PlayPauseClick()
    {
        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher) return;

        // Flip icon first so UI feels instant; network state follows via RPC.
        playImg.sprite = playImg.sprite == playSprites[0] ? playSprites[1] : playSprites[0];

        if (playImg.sprite == playSprites[1])
        {
            TeacherPlay();
        }
        else
        {
            TeacherPause();
        }
    }

    /// <summary>
    /// Teacher: set Playing=true and broadcast to all clients.
    /// </summary>
    public void TeacherPlay()
    {
        RunWithAuthority(() =>
        {
            EnsurePreparedAuthority(() =>
            {
                // If ended, restart at 0
                double startTime = (video.time >= video.length - 0.05f) ? 0 : video.time;

                video.time = startTime;
                video.Play();

                // 🔑 Always set a new reference snapshot and broadcast full state
                SetReferenceNow(startTime, Runner.SimulationTime, true, PlaybackSpeed <= 0 ? 1f : PlaybackSpeed);

                RpcApplyState(BaseVideoTime, BaseServerTime, Playing, PlaybackSpeed); // Force sync Students
                RpcPlayAnimation(VideoActionType.Play);
            });
        });
    }

    /// <summary>
    /// Teacher: set Playing=false and broadcast to all clients.
    /// </summary>
    public void TeacherPause()
    {
        RunWithAuthority(() =>
        {
            EnsurePreparedAuthority(() =>
            {
                SetReferenceNow(video.time, Runner.SimulationTime, false, PlaybackSpeed);
                RpcSetPlayMode(false);
                RpcPlayAnimation(VideoActionType.Pause);  // Broadcast pause animation to all
            });
        });
    }

    /// <summary>
    /// Teacher: seek to a specific time (seconds). Optionally keep playing after the seek.
    /// </summary>
    public void TeacherSeek(double timeSeconds, bool autoPlay = false)
    {
        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher) return;

        if (bufferLoadingMsg) bufferLoadingMsg.SetActive(true);

        timeSeconds = Mathf.Max(0f, (float)timeSeconds); // Clamp to start

        RunWithAuthority(() =>
        {
            EnsurePreparedAuthority(() =>
            {
                // 🔑 If seeking beyond end, treat as ended
                if (video.length > 0 && timeSeconds >= video.length - 0.05f)
                {
                    timeSeconds = video.length;
                    video.time = timeSeconds;
                    video.Pause();

                    SetReferenceNow(timeSeconds, Runner.SimulationTime, false, PlaybackSpeed);
                    RpcApplyState(BaseVideoTime, BaseServerTime, false, PlaybackSpeed);

                    playImg.sprite = playSprites[0];
                    return;
                }

                video.time = timeSeconds; // Perform local seek

                bool shouldPlay = autoPlay || Playing;
                SetReferenceNow(timeSeconds, Runner.SimulationTime, shouldPlay, PlaybackSpeed);

                RpcApplyState(BaseVideoTime, BaseServerTime, shouldPlay, PlaybackSpeed);

                // 🔑 Add this small wait so buffer UI is visible until frame is ready
                StartCoroutine(HideBufferWhenFrameReady(timeSeconds));
            });
        });
    }

    private IEnumerator HideBufferWhenFrameReady(double targetTime)
    {
        float timeout = 2f; // shorter timeout; seek decode should be fast
        float start = Time.time;

        // 🔑 If paused, nudge decoder to refresh texture
        if (!Playing)
            video.StepForward();

        while (video.isPrepared && Time.time - start < timeout)
        {
            // Once Unity has actually moved the playhead near our seek request
            if (System.Math.Abs(video.time - targetTime) <= 0.5)
                break;

            yield return null;
        }

        if (bufferLoadingMsg) bufferLoadingMsg.SetActive(false);
    }

    /// <summary>
    /// Teacher: fast-forward by skipStepSeconds and broadcast animation.
    /// </summary>
    public void TeacherFastForward()
    {
        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher) return;

        // Already at or beyond end → do nothing
        if (video.time >= video.length - 0.05f)
        {
            TeacherSeek(video.length - 0.05f, autoPlay: false); // Ensure consistent state
            RpcPlayAnimation(VideoActionType.FastForward, skipStepSeconds);
            return;
        }

        double newTime = video.time + skipStepSeconds;

        if (video.length > 0)
            newTime = Mathf.Clamp((float)newTime, 0f, (float)(video.length - 0.05f));

        // If we reached end by skipping forward, treat as ended
        if (newTime >= video.length - 0.05f)
        {
            TeacherSeek(video.length - 0.05f, autoPlay: false);
            RpcPlayAnimation(VideoActionType.FastForward, skipStepSeconds);
            return;
        }

        TeacherSeek(newTime, autoPlay: Playing);
        RpcPlayAnimation(VideoActionType.FastForward, skipStepSeconds);
    }

    /// <summary>
    /// Teacher: rewind by skipStepSeconds and broadcast animation.
    /// </summary>
    public void TeacherFastBackward()
    {
        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher) return;

        // Already at or before start → do nothing except rebroadcast state
        if (video.time <= 0.05f)
        {
            TeacherSeek(0, autoPlay: Playing); // Ensure we stay synced at start
            RpcPlayAnimation(VideoActionType.FastBackward, skipStepSeconds);
            return;
        }

        double newTime = video.time - skipStepSeconds;

        if (newTime <= 0.05f)
        {
            // Clamp to start
            newTime = 0;
            TeacherSeek(newTime, autoPlay: Playing);
            RpcPlayAnimation(VideoActionType.FastBackward, skipStepSeconds);
            return;
        }

        if (video.length > 0)
            newTime = Mathf.Clamp((float)newTime, 0f, (float)(video.length - 0.05f));

        TeacherSeek(newTime, autoPlay: Playing);
        RpcPlayAnimation(VideoActionType.FastBackward, skipStepSeconds);
    }

    /// <summary>
    /// Jump to a specific time (teacher only). If clip not prepared, prepare then jump.
    /// </summary>
    public void JumpToTime(double seconds)
    {
        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher) return;

        if (video == null) return;

        if (video.length > 0)
            seconds = Mathf.Clamp((float)seconds, 0f, (float)(video.length - 0.05f));
        else if (seconds < 0) seconds = 0;

        // If not prepared, prepare and start at the requested time (keeps playing state)
        if (!video.isPrepared)
        {
            StartPrepareAndBroadcast(Playing, seconds);
            //Debug.Log($"JumpToTime: video not prepared — starting prepare and will reset to {seconds:F2}s");
            return;
        }

        // Normal path when prepared
        TeacherSeek(seconds, autoPlay: Playing);   // 🔴 this is why it doesn’t pause
        //Debug.Log($"JumpToTime: seeking to {seconds:F2}s");

        playImg.sprite = playSprites[0];
    }

    /// <summary>
    /// Jump to the next saved timestamp after the current playhead.
    /// If already at or beyond the last timestamp, stays at the last.
    /// </summary>
    public void JumpToNextTimeframe()
    {
        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher) return;
        if (video == null || videoListSO.videos[SelectedIndex].jumpTimes == null || videoListSO.videos[SelectedIndex].jumpTimes.Length == 0) return;

        double current = video.isPrepared ? video.time : 0.0;
        double target, delta;

        // 🔑 force pause first
        TeacherPause();

        for (int i = 0; i < videoListSO.videos[SelectedIndex].jumpTimes.Length; i++)
        {
            if (videoListSO.videos[SelectedIndex].jumpTimes[i] > current + 0.5f)
            {
                JumpToTime(videoListSO.videos[SelectedIndex].jumpTimes[i]);
                target = videoListSO.videos[SelectedIndex].jumpTimes[i];
                delta = target - current;
                // Show jump feedback using dynamic skip seconds
                RpcPlayAnimation(VideoActionType.FastForward, delta);
                return;
            }
        }

        JumpToTime(videoListSO.videos[SelectedIndex].jumpTimes[^1]);
        target = videoListSO.videos[SelectedIndex].jumpTimes[^1];
        delta = target - current;
        // Show jump feedback using dynamic skip seconds
        RpcPlayAnimation(VideoActionType.FastForward, delta);
    }

    /// <summary>
    /// Jump to the previous saved timestamp before the current playhead.
    /// If already before or at the first timestamp, stays at the first.
    /// </summary>
    public void JumpToPreviousTimeframe()
    {
        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher) return;
        if (video == null || videoListSO.videos[SelectedIndex].jumpTimes == null || videoListSO.videos[SelectedIndex].jumpTimes.Length == 0) return;

        double current = video.isPrepared ? video.time : 0.0;
        double target, delta;

        // 🔑 force pause first
        TeacherPause();

        for (int i = videoListSO.videos[SelectedIndex].jumpTimes.Length - 1; i >= 0; i--)
        {
            if (videoListSO.videos[SelectedIndex].jumpTimes[i] < current - 0.5f)
            {
                JumpToTime(videoListSO.videos[SelectedIndex].jumpTimes[i]);
                target = videoListSO.videos[SelectedIndex].jumpTimes[i];
                delta = current - target;
                // Show jump feedback using dynamic skip seconds
                RpcPlayAnimation(VideoActionType.FastBackward, delta);
                return;
            }
        }

        JumpToTime(videoListSO.videos[SelectedIndex].jumpTimes[0]);
        target = videoListSO.videos[SelectedIndex].jumpTimes[0];
        delta = target - current;
        // Show jump feedback using dynamic skip seconds
        RpcPlayAnimation(VideoActionType.FastBackward, delta);
    }

    /// <summary>
    /// Teacher: set volume (0..1) and broadcast to students.
    /// </summary>
    public void TeacherSetVolume(float volume)
    {
        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher) return;

        RunWithAuthority(() =>
        {
            volume = Mathf.Clamp01(volume);

            Volume = volume;
            ApplyAudioLocally();

            RpcApplyAudio(volume, Muted);
        });
    }

    /// <summary>
    /// Teacher: toggle mute/unmute and broadcast to students.
    /// </summary>
    public void TeacherToggleMute()
    {
        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher) return;

        RunWithAuthority(() =>
        {
            Muted = !Muted;

            if (Muted)
                tempVolume = Volume;

            Volume = Muted ? 0 : tempVolume;

            volumeSlider.value = Volume;

            ApplyAudioLocally();

            RpcApplyAudio(Volume, Muted);
        });
    }

    public void BackMenuClick()
    {
        videoMenuPanel.SetActive(false);
        videoPlayerPanel.SetActive(true);

        Multiplayer_SoundManager.Instance.PlayClick();
    }
    #endregion

    #region Internals
    private void ApplyAudioLocally()
    {
        if (video != null)
        {
            video.SetDirectAudioVolume(0, Volume);
            video.SetDirectAudioMute(0, Muted);

            muteImg.sprite = Muted ? muteSprites[1] : muteSprites[0];

            if (!Object.HasStateAuthority)
                volumeSlider.value = Volume;
        }
    }

    /// <summary>
    /// Student-side sync loop. Chases authority time with either a hard snap (rare)
    /// or a soft temporary speed adjustment (common for small drift).
    /// </summary>
    private void StepClientSync()
    {
        if (!video.isPrepared) return; // Nothing to do until clip is prepared

        double expected = ComputeExpectedTime(Runner.SimulationTime); // Authority time we should be at now
        double diff = expected - video.time;                           // Positive = we're behind

        // Mirror play/pause locally.
        if (Playing && !video.isPlaying) video.Play();
        if (!Playing && video.isPlaying) video.Pause();

        // Keep base speed applied when not correcting drift.
        if (Mathf.Abs(video.playbackSpeed - PlaybackSpeed) > 0.001f)
            video.playbackSpeed = PlaybackSpeed;

        double abs = System.Math.Abs(diff);

        // Hard snap only if far out of sync, and not more often than every 0.5s.
        if (abs > hardSnapThreshold && Runner.SimulationTime - lastSnapTime > 0.5)
        {
            //Debug.Log($"Hard snap from {video.time:F2} to {expected:F2}");
            video.time = expected;
            lastSnapTime = Runner.SimulationTime; // throttle snaps to avoid freezing on same frame
        }
        else if (abs > softCorrectionThreshold && maxRateDrift > 0f && Playing)
        {
            // Apply a small speed offset so we converge smoothly to authority.
            float drift = Mathf.Clamp((float)(diff * 0.5), -maxRateDrift, maxRateDrift);
            video.playbackSpeed = PlaybackSpeed + drift;
        }
        else
        {
            // If we are within tolerance, make sure we are at the authoritative base speed.
            if (Mathf.Abs(video.playbackSpeed - PlaybackSpeed) > 0.001f)
                video.playbackSpeed = PlaybackSpeed;
        }

        if (video.time >= video.length - 0.05f)
        {
            video.Pause();
        }

        // Keep Play/Pause icon up to date locally.
        playImg.sprite = video.isPlaying ? playSprites[1] : playSprites[0];
    }

    /// <summary>
    /// Computes where the authority expects the video to be now given the stored reference.
    /// </summary>
    private double ComputeExpectedTime(double simTime)
    {
        if (!Playing) return BaseVideoTime;

        double dt = simTime - BaseServerTime;
        if (dt < 0) dt = 0;

        double expected = BaseVideoTime + dt * PlaybackSpeed;

        // 🔑 Clamp to last frame minus small epsilon to avoid looping to 0
        if (video != null && video.clip != null)
        {
            expected = Mathf.Min((float)expected, (float)(video.length - 0.05f));
        }

        return expected;
    }

    /// <summary>
    /// Starts a local prepare step and then applies the current replicated state.
    /// Called on Students when clip/source changes.
    /// </summary>
    private void ApplyNetworkStateImmediate()
    {
        if (_prepareRoutine != null) StopCoroutine(_prepareRoutine);
        _prepareRoutine = StartCoroutine(CoPrepareThenApply());
    }

    /// <summary>
    /// Client-side prepare coroutine: set correct source, wait for prepare,
    /// jump to expected time, apply speed, and mirror play/pause.
    /// </summary>
    private IEnumerator CoPrepareThenApply()
    {
        if (bufferLoadingMsg) bufferLoadingMsg.SetActive(true);

        video.Stop();
        video.clip = null;
        video.url = string.Empty;

        // In CoPrepareThenApply() (client-side prepare)
        if (UseUrl)
        {
            video.source = VideoSource.Url;
            video.url = ClipUrl.ToString();
        }
        else
        {
            video.source = VideoSource.VideoClip;
            video.clip = ResolveClipByIndex(SelectedIndex);   // <-- important!
        }

        _isPrepared = false;
        video.Prepare();

        float timeout = 10f;
        float start = Time.time;
        while (!_isPrepared && Time.time - start < timeout)
            yield return null;

        if (!_isPrepared)
        {
            //Debug.LogError("Student: Video prepare timed out!");
            if (bufferLoadingMsg) bufferLoadingMsg.SetActive(false);
            yield break;
        }

        double expected = ComputeExpectedTime(Runner.SimulationTime);
        video.time = expected;
        video.playbackSpeed = PlaybackSpeed;
        if (Playing) video.Play(); else video.Pause();

        // 🔑 Sync audio
        ApplyAudioLocally();

        // 🔑 Wait until the local frame is ready before hiding buffering
        yield return StartCoroutine(HideBufferWhenFrameReady(expected));
    }

    // Helper
    private VideoClip ResolveClipByIndex(int i)
    {
        if (videoListSO.videos != null && i >= 0 && i < videoListSO.videos.Length)
            return videoListSO.videos[i].videoClip;

        //Debug.LogWarning($"[FusionVideoSync] SelectedIndex {i} is out of range for videoClips.");
        return null;
    }

    /// <summary>
    /// Teacher-only prepare helper: load source, then either play or pause at a reset time and
    /// broadcast the updated reference so Students converge immediately.
    /// </summary>
    private void StartPrepareAndBroadcast(bool keepPlaying, double resetTimeTo)
    {
        if (_prepareRoutine != null) StopCoroutine(_prepareRoutine);
        _prepareRoutine = StartCoroutine(CoAuthorityPrepare(keepPlaying, resetTimeTo));
    }

    /// <summary>
    /// Authority prepare coroutine: set source, wait for prepare, apply reset time, then
    /// set reference + send RpcApplyState so Students follow.
    /// </summary>
    private IEnumerator CoAuthorityPrepare(bool keepPlaying, double resetTime)
    {
        if (bufferLoadingMsg) bufferLoadingMsg.SetActive(true);

        video.Stop();                // ensure clean state
        video.clip = null;           // clear old source
        video.url = string.Empty;

        // In CoAuthorityPrepare(...) (teacher-side prepare)
        if (UseUrl)
        {
            video.source = VideoSource.Url;
            video.url = ClipUrl.ToString();
        }
        else
        {
            video.source = VideoSource.VideoClip;
            video.clip = ResolveClipByIndex(SelectedIndex);   // <-- ensure Teacher has the same clip
        }

        _isPrepared = false;
        video.Prepare();

        float timeout = 10f; // safety fallback
        float start = Time.time;

        while (!_isPrepared && Time.time - start < timeout)
        {
            //Debug.Log("Preparing video...");
            yield return null;
        }

        if (!_isPrepared)
        {
            //Debug.LogError("Video prepare timed out!");
            if (bufferLoadingMsg) bufferLoadingMsg.SetActive(false);
            yield break;
        }

        video.time = resetTime;
        if (keepPlaying)
        {
            SetReferenceNow(resetTime, Runner.SimulationTime, true, PlaybackSpeed <= 0 ? 1f : PlaybackSpeed);
            video.playbackSpeed = PlaybackSpeed;
            video.Play();
            RpcApplyState(BaseVideoTime, BaseServerTime, Playing, PlaybackSpeed);
        }
        else
        {
            SetReferenceNow(resetTime, Runner.SimulationTime, false, PlaybackSpeed <= 0 ? 1f : PlaybackSpeed);
            video.Pause();
            RpcApplyState(BaseVideoTime, BaseServerTime, Playing, PlaybackSpeed);
        }

        if (bufferLoadingMsg) bufferLoadingMsg.SetActive(false);
    }

    /// <summary>
    /// Helper: if not prepared yet, kick off prepare flow (authority) and return.
    /// Otherwise, run onReady immediately.
    /// </summary>
    private void EnsurePreparedAuthority(System.Action onReady)
    {
        if (!video.isPrepared)
        {
            StartPrepareAndBroadcast(false, 0);
            return;
        }
        onReady?.Invoke();
    }

    /// <summary>
    /// Store a new reference snapshot (video time + server time) and state flags.
    /// </summary>
    private void SetReferenceNow(double videoTime, double serverTime, bool playing, float speed)
    {
        BaseVideoTime = videoTime;
        BaseServerTime = serverTime;
        Playing = playing;
        PlaybackSpeed = speed;
    }
    #endregion

    #region Fusion Callbacks
    /// <summary>
    /// When clip source or URL changes, clients re-prepare and apply the networked state.
    /// </summary>
    private void OnClipChanged()
    {
        ApplyNetworkStateImmediate();
    }

    private void OnSelectedIndexChanged()
    {
        // When index changes (and we're using local clips), re-prepare on clients
        if (!UseUrl) ApplyNetworkStateImmediate();
    }

    private void OnSeekChanged() { /* Hook exists for extensibility; left intentionally blank. */ }

    /// <summary>
    /// When Playing or speed changes, ensure local VideoPlayer matches (only if already prepared).
    /// </summary>
    private void OnStateChanged()
    {
        if (video.isPrepared)
        {
            //Debug.Log("OnStateChanged - " + Playing);
            if (Playing && !video.isPlaying) video.Play();
            if (!Playing && video.isPlaying) video.Pause();
            video.playbackSpeed = PlaybackSpeed;
        }
    }

    private void OnVolumeChanged()
    {
        ApplyAudioLocally();
    }

    private void OnMuteChanged()
    {
        ApplyAudioLocally();
    }
    #endregion

    #region RPCs
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RpcApplyAudio(float volume, bool muted, RpcInfo info = default)
    {
        if (info.IsInvokeLocal) return; // Teacher already applied locally

        Volume = volume;
        Muted = muted;
        ApplyAudioLocally();
    }

    // SECURITY NOTE: RPC sources are currently RpcSources.All to match your original code.
    // For stricter control, consider using RpcSources.StateAuthority so only the Teacher can
    // broadcast state changes. Left as-is per your request (no removals or behavior change).

    /// <summary>
    /// Broadcast the authoritative reference snapshot and state flags.
    /// Students pick this up and adjust playback accordingly.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RpcApplyState(double baseVideoTime, double baseServerTime, bool playing, float speed, RpcInfo info = default)
    {
        BaseVideoTime = baseVideoTime;
        BaseServerTime = baseServerTime;
        Playing = playing;
        PlaybackSpeed = speed;

        if (!Object.HasStateAuthority)
        {
            // 🔹 Students: show buffering while jumping to new time
            if (bufferLoadingMsg) bufferLoadingMsg.SetActive(true);

            video.time = baseVideoTime;
            video.playbackSpeed = speed;
            if (playing) video.Play(); else video.Pause();

            // 🔹 Wait until their local frame is actually ready
            StartCoroutine(HideBufferWhenFrameReady(baseVideoTime));
        }
    }

    /// <summary>
    /// Broadcast a play/pause mode toggle. Also captures a fresh server timestamp.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RpcSetPlayMode(bool playing, RpcInfo info = default)
    {
        Playing = playing;
        BaseVideoTime = video.isPrepared ? video.time : BaseVideoTime;
        BaseServerTime = Runner.SimulationTime;
    }

    /// <summary>
    /// Broadcast which feedback animation to play on all peers.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RpcPlayAnimation(VideoActionType action, double seconds = 0)
    {
        switch (action)
        {
            case VideoActionType.Play:
                PlayTweenAnimation();
                break;
            case VideoActionType.Pause:
                PauseTweenAnimation();
                break;
            case VideoActionType.FastForward:
                FastForwardTweenAnimation(seconds);
                break;
            case VideoActionType.FastBackward:
                FastBackwardTweenAnimation(seconds);
                break;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_HideMenu(RpcInfo info = default)
    {
        if (info.IsInvokeLocal)
        {
            return;
        }

        //Debug.LogError("RPC_MutePlayer - " + playerIds.Length + " : " + mute);

        videoMenuPanel.SetActive(false);
        videoPlayerPanel.SetActive(true);
    }
    #endregion

    #region Video Events
    /// <summary>
    /// Logs errors from the VideoPlayer for easier diagnostics in the Editor/console.
    /// </summary>
    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"[FusionVideoSync] Video error: {message}");
    }

    /// <summary>
    /// Mark as prepared so coroutines waiting on prepare can proceed.
    /// </summary>
    private void OnVideoPrepared(VideoPlayer source)
    {
        _isPrepared = true;
    }

    /// <summary>
    /// Called when Teacher's video reaches the end.
    /// Broadcasts a pause so all students stop too.
    /// </summary>
    private void OnVideoEndReached(VideoPlayer source)
    {
        if (!Object.HasStateAuthority) return;

        Playing = false;
        video.Pause();

        double endTime = video.length > 0 ? video.length - 0.05f : 0;

        // 🔑 Important: keep consistent with Students’ clamp
        SetReferenceNow(endTime, Runner.SimulationTime, false, PlaybackSpeed);

        RpcApplyState(BaseVideoTime, BaseServerTime, false, PlaybackSpeed);

        playImg.sprite = playSprites[0];
    }
    #endregion

    #region Animations
    private void PlayTweenAnimation() => AnimateFeedback(playAnim);
    private void PauseTweenAnimation() => AnimateFeedback(pauseAnim);
    private void FastForwardTweenAnimation(double seconds)
    {
        fastForwardTxt.text = $"{seconds:F0} sec";
        AnimateFeedback(fastForwardAnim);
    }

    private void FastBackwardTweenAnimation(double seconds)
    {
        fastBackwardTxt.text = $"{seconds:F0} sec";
        AnimateFeedback(fastBackwardAnim);
    }

    private void AnimateFeedback(RectTransform target)
    {
        DOTween.Kill(target);
        target.localScale = Vector3.one * 0.75f;
        target.DOScale(Vector3.one, animationDuration).OnComplete(() => target.localScale = Vector3.zero);
    }
    #endregion

    #region Context Menu

    public void MenuClick()
    {
        if (FusionLobbyManager.Instance.playerMode != PlayerMode.Teacher) return;

        videoMenuPanel.SetActive(true);
        videoPlayerPanel.SetActive(false);

        ListVideo();

        // ✅ Pause video if it was playing (Teacher only)
        if (Playing)
        {
            TeacherPause();

            playImg.sprite = playSprites[0];
        }

        Multiplayer_SoundManager.Instance.PlayClick();
    }

    void ListVideo()
    {
        for (int i = 0; i < videoSelectViews.Count; i++)
        {
            videoSelectViews[i].Off();
        }

        for (int i = 0; i < videoListSO.videos.Length; i++)
        {
            if (videoSelectViews.Count <= i)
            {
                videoSelectViews.Add(Instantiate(videoSelectViewPrefab, videoSelectScrollRect.content));
            }

            videoSelectViews[i].Bind(videoListSO.videos[i].videoName, videoListSO.videos[i].thumbnail, SelectVideo);
        }
    }

    void SelectVideo(int index)
    {
        videoMenuPanel.SetActive(false);
        videoPlayerPanel.SetActive(true);

        RunWithAuthority(() =>
        {
            if (index < 0 || index >= videoListSO.videos.Length)
            {
                Debug.LogWarning($"[FusionVideoSync] SelectVideo out of range: {index}");
                return;
            }

            var entry = videoListSO.videos[index];

            // 🔑 Reset networked state
            SelectedIndex = index;
            Playing = false;
            PlaybackSpeed = 1f;
            BaseVideoTime = 0;
            BaseServerTime = Runner.SimulationTime;

            Volume = 1f;
            Muted = false;
            volumeSlider.value = Volume;

            // 🔑 Branch between URL and local clip
            if (entry.useURL && !string.IsNullOrEmpty(entry.videoURL))
            {
                UseUrl = true;
                ClipUrl = entry.videoURL;

                video.source = VideoSource.Url;
                video.url = entry.videoURL;
            }
            else
            {
                UseUrl = false;
                ClipUrl = default;

                video.source = VideoSource.VideoClip;
                video.clip = entry.videoClip;
            }

            video.time = 0;
            video.playbackSpeed = 1f;
            video.Pause();

            // Update UI
            playImg.sprite = playSprites[0];

            // Broadcast reset so Students sync immediately
            StartPrepareAndBroadcast(false, 0);

            RPC_HideMenu();

            //Debug.Log($"Teacher selected NEW video: {videoListSO.videos[index].videoName} (reset state synced)");
        });

        Multiplayer_SoundManager.Instance.PlayClick();
    }

    #endregion

    #region Authority Helpers

    // 🔹 ADDED: Coroutine reference to manage authority waiting
    private Coroutine _waitAuthorityRoutine;

    /// <summary>
    /// Ensures that the given action is executed only if this client has StateAuthority.
    /// If authority is missing, it will request and then wait until authority is granted,
    /// and execute the action once authority is available.
    /// </summary>
    private void RunWithAuthority(System.Action action)
    {
        if (Object.HasStateAuthority)
        {
            action?.Invoke();
        }
        else
        {
            Object.RequestStateAuthority();
            if (_waitAuthorityRoutine != null) StopCoroutine(_waitAuthorityRoutine);
            _waitAuthorityRoutine = StartCoroutine(WaitForAuthorityThen(action));
        }
    }

    /// <summary>
    /// Coroutine that waits until StateAuthority is acquired before executing the action.
    /// Includes a timeout to prevent hanging forever if authority is not granted.
    /// </summary>
    private IEnumerator WaitForAuthorityThen(System.Action action)
    {
        float timeout = 2f; // safeguard
        float elapsed = 0f;

        while (!Object.HasStateAuthority && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (Object.HasStateAuthority)
            action?.Invoke();

        _waitAuthorityRoutine = null;
    }

    #endregion
}