using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Rlcm.Game;

namespace Rlcm.Windows
{
    public partial class MainWindow
    {
        private readonly Challenge _challenge;
        private readonly Bundle _bundle;
        private readonly Memory _memory;

        private readonly Stopwatch _updateDelay;
        private bool _delayedUpdate;

        private bool _online;

        public MainWindow()
        {
            _memory = new Memory();
            _challenge = new Challenge(_memory);
            _bundle = new Bundle();
            _updateDelay = new Stopwatch();

            _memory.OnProcessOpened = process =>
            {
                var filename = process.MainModule?.FileName;
                var location = filename?.Substring(0, filename.LastIndexOf('\\'));
                _bundle.SetLocation(location);
                InstallMod.IsChecked = _bundle.IsModInstalled();
            };

            InitializeComponent();
            UpdateFields();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            InstallMod.IsChecked = _bundle.IsModInstalled();
            _memory.Load();

            var timer = new DispatcherTimer();
            timer.Tick += OnTimerTick;
            timer.Interval = new TimeSpan(5000000);
            timer.Start();
        }

        private void UpdateFields()
        {
            // check if we are in a challenge
            if (!_challenge.Load())
            {
                NoChallenge.Visibility = Visibility.Visible;
                return;
            }

            NoChallenge.Visibility = Visibility.Hidden;

            if (_updateDelay.ElapsedMilliseconds >= 1000)
                _delayedUpdate = false;

            Level.Text = _challenge.GetLevel() switch
            {
                "spikyroad" => "The Neverending Pit",
                "run" => "Land of the Livid Dead",
                "goingup" => "The Infinite Tower",
                "drc" => "Murfy's Dungeon",
                "shaolin" => "The Dojo",
                _ => "Unknown level"
            };

            Difficulty.Text = "Difficulty: " + _challenge.GetDifficulty() switch
            {
                "expert" => "Expert",
                "normal" => "Normal",
                _ => "Unknown"
            };

            _online = _challenge.IsOnline();
            Mode.Text = "Mode: " + (_online ? "Online" : "Training");

            // we assume that the challenge isn't ready as long as both the
            // challenge seed and generator seed don't share the same value
            var seed = _challenge.GetChallengeSeed();
            var challengeReady = seed == _challenge.GetGeneratorSeed();
            Seed.IsEnabled = !_online && challengeReady;
            Random.IsEnabled = Seed.IsEnabled;

            if (!Seed.IsKeyboardFocused || !_delayedUpdate)
                Seed.Text = FormatSeed(seed);

            var type = _challenge.GetChallengeType();
            Type.SelectedIndex = TypeToIndex(type);
            Type.IsEnabled = !_online;

            GoalUnit.Text = type switch
            {
                Challenge.Type.GrabThemQuickly => "lums",
                Challenge.Type.GetThereQuickly => "meters",
                Challenge.Type.AgainstTheClock => "sec",
                Challenge.Type.GrabThereQuickly2 => "sec",
                _ => ""
            };

            LimitUnit.Text = type switch
            {
                Challenge.Type.AsFarAsYouCan => "meters",
                Challenge.Type.GrabThemQuickly => "sec",
                Challenge.Type.GetThereQuickly => "sec",
                Challenge.Type.AgainstTheClock => "meters",
                Challenge.Type.AsManyAsYouCan => "lums",
                Challenge.Type.GrabThereQuickly2 => "lums",
                _ => ""
            };

            if (type == Challenge.Type.AsFarAsYouCan || type == Challenge.Type.AsManyAsYouCan)
            {
                Goal.IsEnabled = false;
                Goal.Text = "n/a";
            }
            else if (!Goal.IsKeyboardFocused || !_delayedUpdate)
            {
                Goal.IsEnabled = !_online;
                Goal.Text = _challenge.GetGoal().ToString(CultureInfo.InvariantCulture);
            }

            if (!Limit.IsKeyboardFocused || !_delayedUpdate)
                Limit.Text = _challenge.GetLimit().ToString(CultureInfo.InvariantCulture);

            Limit.IsEnabled = !_online;
        }

        private void DelayUpdate()
        {
            _delayedUpdate = true;
            _updateDelay.Restart();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            UpdateFields();
        }

        private void OnChangeType(object sender, EventArgs args)
        {
            if (_online)
                return;

            var type = TypeToIndex(Type.SelectedIndex);
            _challenge.SetChallengeType(type);
            UpdateFields();
        }

        private void OnChangeSeed(object sender, EventArgs args)
        {
            if (_online || !Seed.IsKeyboardFocused)
                return;

            DelayUpdate();

            if (!Seed.Text.Any())
                return;

            try
            {
                var seed = Convert.ToUInt32(Seed.Text.Replace(" ", ""), 16);
                seed = (seed & 0x000000FF) << 24 |
                       (seed & 0x0000FF00) << 8 |
                       (seed & 0x00FF0000) >> 8 |
                       (seed & 0xFF000000) >> 24;

                _challenge.SetSeed((int) seed);
            }
            catch (FormatException)
            {
                // ignore if ill-formed
            }
            catch (OverflowException)
            {
                // ignore if too large
            }
        }

        private void OnChangeGoal(object sender, EventArgs args)
        {
            if (_online || !Goal.IsKeyboardFocused)
                return;

            DelayUpdate();

            try
            {
                var goal = float.Parse(Goal.Text);
                _challenge.SetGoal(goal);
            }
            catch (FormatException)
            {
                // ignore if ill-formed
            }
            catch (OverflowException)
            {
                // ignore if too large
            }
        }

        private void OnChangeLimit(object sender, EventArgs args)
        {
            if (_online || !Limit.IsKeyboardFocused)
                return;

            DelayUpdate();

            try
            {
                var limit = float.Parse(Limit.Text);
                _challenge.SetLimit(limit);
            }
            catch (FormatException)
            {
                // ignore if ill-formed
            }
            catch (OverflowException)
            {
                // ignore if too large
            }
        }

        private void OnRandomSeed(object sender, EventArgs args)
        {
            if (_online)
                return;

            var seed = new Random().Next();
            _challenge.SetSeed(seed);
            Seed.Text = FormatSeed(seed);
        }

        private void OnInstallMod(object sender, EventArgs args)
        {
            var install = InstallMod.IsChecked == true;
            if (_bundle.NotFound())
            {
                InstallMod.IsChecked = !install;
                MessageBox.Show("Cannot find the game's installation folder. Please start the game first.",
                    "Install location not found", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            try
            {
                _bundle.InstallTrainingMod(install);
            }
            catch (IOException e)
            {
                InstallMod.IsChecked = !install;
                MessageBox.Show(e.Message, "Cannot open bundle", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string FormatSeed(int seed)
        {
            var bytes = BitConverter.GetBytes(seed);
            return string.Join(" ", bytes.Select(x => x.ToString("X2")));
        }

        private static int TypeToIndex(int type)
        {
            return type switch
            {
                0 => 2,
                2 => 0,
                3 => 4,
                4 => 3,
                _ => type
            };
        }
    }
}
