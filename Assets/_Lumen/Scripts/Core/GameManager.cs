using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Lumen
{
    /// <summary>
    /// State machine: Intro (Catmull-Rom fly-through, skippable) -> Playing -> Paused -> Won / Lost.
    /// Owns health, timer, shard count and score, and bootstraps the world.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public enum GameState { Intro, Playing, Paused, Won, Lost }

        public const int MaxHealth = 3;
        public const float TimeLimit = 180f;
        public const float IntroDuration = 11f;

        public OrbitCamera orbitCam;
        public WorldBuilder world;
        [Tooltip("Debug: Sentinels and the timer cannot hurt the player (used by automated verification).")]
        public bool godMode;

        public GameState State { get; private set; } = GameState.Intro;
        public int ShardsCollected { get; private set; }
        public int Health { get; private set; } = MaxHealth;
        public float TimeLeft { get; private set; } = TimeLimit;
        public int Score { get; private set; }
        public string ToastText { get; private set; } = "";
        public float HitFlash { get; private set; }

        float toastTimer, introT;
        Vector3[] introPath;
        public Vector3[] IntroPath => introPath;
        GameState stateBeforePause = GameState.Playing;

        void Awake() { I = this; Time.timeScale = 1f; Application.runInBackground = true; }

        void Start()
        {
            world.Build(Random.Range(1, 9999));
            ParticleFactory.Dust(orbitCam.transform);
            BeginIntro();
        }

        // ---------------------------------------------------------------- flow
        void BeginIntro()
        {
            State = GameState.Intro;
            introT = 0f;
            introPath = MakeIntroPath(world.StartPos);
            orbitCam.Follow = false;
            if (CurveVisualizer.I != null) CurveVisualizer.I.Rebuild();
            Toast("Collect all 12 Light Shards, then reach the Portal.  (SPACE skips intro)", 5f);
        }

        public static Vector3[] MakeIntroPath(Vector3 orb)
        {
            var pts = new Vector3[8];
            for (int i = 0; i < pts.Length; i++)
            {
                float k = i / (float)(pts.Length - 1);
                float ang = Mathf.Lerp(0.3f, 5.6f, k);
                float rad = Mathf.Lerp(78f, 22f, k * k);
                float x = Mathf.Cos(ang) * rad, z = Mathf.Sin(ang) * rad;
                float y = TerrainGenerator.HeightAt(x, z) + Mathf.Lerp(34f, 9f, k);
                pts[i] = new Vector3(x, y, z);
            }
            pts[pts.Length - 1] = orb + new Vector3(0, 6f, -8f);
            return pts;
        }

        void StartPlaying()
        {
            State = GameState.Playing;
            orbitCam.Follow = true;
            orbitCam.Place(true);
        }

        public void ResetRun()
        {
            ShardsCollected = 0; Health = MaxHealth; TimeLeft = TimeLimit; Score = 0; HitFlash = 0;
        }

        public void Regenerate()
        {
            ResetRun();
            world.Build(Random.Range(1, 9999));
            StartPlaying();
            Toast("World regenerated  (seed " + world.Seed + ")", 2.5f);
        }

        public void Toast(string text, float seconds) { ToastText = text; toastTimer = seconds; }

        // ---------------------------------------------------------------- gameplay events
        public void CollectShard()
        {
            ShardsCollected++;
            Toast("Shard " + ShardsCollected + "/" + WorldBuilder.ShardCount, 1.2f);
            if (ShardsCollected >= WorldBuilder.ShardCount) world.Portal.Open();
        }

        public void HitPlayer(Vector3 from)
        {
            if (State != GameState.Playing || godMode) return;
            if (!world.OrbCtrl.TakeHit(from)) return;
            Health--;
            HitFlash = 1f;
            if (DebugViews.I != null) DebugViews.I.TriggerHit();
            if (Health <= 0) Finish(false); else Toast("Hit!  " + Health + " left", 1.2f);
        }

        public void WinGame() { if (State == GameState.Playing) Finish(true); }

        void Finish(bool won)
        {
            Score = Mathf.RoundToInt(Mathf.Max(0f, TimeLeft) * 10f + ShardsCollected * 100f);
            State = won ? GameState.Won : GameState.Lost;
        }

        public void Restart() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        public void ToMenu() { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); }
        public void Resume() { if (State == GameState.Paused) State = stateBeforePause; }

        // ---------------------------------------------------------------- update
        void Update()
        {
            if (toastTimer > 0f) { toastTimer -= Time.unscaledDeltaTime; if (toastTimer <= 0f) ToastText = ""; }
            if (HitFlash > 0f) HitFlash = Mathf.Max(0f, HitFlash - Time.unscaledDeltaTime * 1.8f);

            bool labOpen = LabPanel.I != null && LabPanel.I.Open;

            switch (State)
            {
                case GameState.Intro:
                    UpdateIntro();
                    break;
                case GameState.Playing:
                    if (!godMode) TimeLeft -= Time.deltaTime;
                    if (TimeLeft <= 0f) { TimeLeft = 0f; Finish(false); }
                    break;
            }

            if (LumenInput.Pressed(Key.Escape))
            {
                if (labOpen) LabPanel.I.Open = false;
                else if (State == GameState.Playing || State == GameState.Intro) { stateBeforePause = State; State = GameState.Paused; }
                else if (State == GameState.Paused) State = stateBeforePause;
            }

            // time scale: pause = 0, Lab Panel open = 0.25 (slow-mo so the examiner can observe), else 1
            Time.timeScale = State == GameState.Paused ? 0f : (labOpen && (State == GameState.Playing || State == GameState.Intro) ? 0.25f : 1f);

            bool wantLock = State == GameState.Playing && !labOpen;
            Cursor.lockState = wantLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !wantLock;
        }

        void UpdateIntro()
        {
            introT += Time.unscaledDeltaTime * (Time.timeScale > 0 ? 1f : 0f);
            bool skip = LumenInput.Pressed(Key.Space) || LumenInput.Pressed(Key.Enter) || LumenInput.Fire();
            if (introT >= IntroDuration || skip) { StartPlaying(); return; }

            float u = Easing.EaseInOut(introT / IntroDuration) * (introPath.Length - 1);
            Vector3 p = CatmullRom.EvaluatePath(introPath, u);
            orbitCam.transform.position = p;
            Vector3 look = world.Orb.position + Vector3.up * 2f;
            orbitCam.transform.rotation = QuaternionMath.LookRotation(look - p, Vector3.up);
        }
    }
}
