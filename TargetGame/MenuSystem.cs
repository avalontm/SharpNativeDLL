using AvalonInjectLib;
using AvalonInjectLib.UIFramework;
using static AvalonInjectLib.Structs;

namespace TargetGame
{
    public class MenuSystem : UIFrameworkRenderSystem
    {
        // Constantes de memoria
        const int PLAYER_MOVE_TO = 0xA51220;
        const int PLAYER_SCORE = 0xEFFE54;
        const int PLAYER_HEALTH = 0xEFFF00;
        const int PLAYER_LEVEL = 0xFAFE70;
        const int PLAYER_NAME = 0x001F008;

        // Proceso y estado
        public ProcessEntry Process { get; set; }
        private bool _isInitialized = false;
        private bool _isOrcWalking = false;

        // UI Elements
        public Window MainWindow { get; private set; }
        private TabControl _tabControl;
        private Label _statusLabel;

        // Player Info Controls
        private Slider _scoreSlider;
        private Slider _healthSlider;
        private Slider _levelSlider;
        private TextBox _playerNameTextBox;
        private Button _applyButton;

        // OrcWalk Controls
        private TextBox _walkPointsTextBox;
        private Button _orcWalkButton;
        private float _walkSpeed = 1.0f;
        private List<Vector2> _walkPoints = new List<Vector2>();
        private int _currentWalkPoint = 0;
        private DateTime _lastWalkTime;

        public void Initialize(uint processId)
        {
            if (_isInitialized) return;

            Font.Initialize();
            InputSystem.Initialize(processId);
            CreateUI();
            _isInitialized = true;
        }

        private void CreateUI()
        {
            // Configurar ventana principal
            MainWindow = new Window
            {
                Title = "Game Controller",
                Width = 600,
                Height = 500,
                BackColor = new Color(30, 30, 30)
            };

            // Crear TabControl
            _tabControl = new TabControl
            {
                Width = 580,
                Height = 430
            };

            // Añadir pestañas
            CreatePlayerInfoTab();
            CreateOrcWalkTab();

            // Status label
            _statusLabel = new Label
            {
                Y = 440,
                Width = 580,
                Height = 20,
                ForeColor = Color.FromArgb(200, 200, 200)
            };

            // Añadir elementos a la ventana
            MainWindow.Content = _tabControl;

            // Configurar eventos
            SetupEvents();
 
        }

        private void CreatePlayerInfoTab()
        {
            var playerTab = new TabPage("Player Info");
            int yPos = 10;
            int panelHeight = 60; // Reducido ya que no necesitamos espacio para el label extra
            int spacing = 10;

            // Score Control
            _scoreSlider = CreateSlider(10, yPos, "Score", Color.FromArgb(255, 215, 0), 0, 999999);
            playerTab.AddChild(_scoreSlider);
            yPos += panelHeight + spacing;

            // Health Control
            _healthSlider = CreateSlider(10, yPos, "Health", Color.FromArgb(255, 100, 100), 1, 9999);
            playerTab.AddChild(_healthSlider);
            yPos += panelHeight + spacing;

            // Level Control
            _levelSlider = CreateSlider(10, yPos, "Level", Color.FromArgb(100, 149, 237), 1, 18);
            playerTab.AddChild(_levelSlider);
            yPos += panelHeight + spacing;

            // Name Control
            var namePanel = new Panel
            {
                X = 10,
                Y = yPos,
                Width = 560,
                Height = 80
            };

            namePanel.AddChild(new Label
            {
                Text = "Player Name:",
                ForeColor = Color.FromArgb(144, 238, 144),
                Width = 200,
                Y = 10
            });

            _playerNameTextBox = new TextBox
            {
                Y = 40,
                Width = 300,
                Height = 30,
                Text = "Player"
            };
            namePanel.AddChild(_playerNameTextBox);
            playerTab.AddChild(namePanel);
            yPos += 90;

            // Apply Button
            _applyButton = new Button
            {
                Y = yPos,
                Width = 560,
                Height = 40,
                Text = "Apply Changes",
                BackColor = Color.FromArgb(0, 180, 0)
            };
            playerTab.AddChild(_applyButton);

            _tabControl.AddTab(playerTab);
        }

        private void CreateOrcWalkTab()
        {
            var orcWalkTab = new TabPage("OrcWalk");
            int yPos = 10;
            int panelHeight = 60;
            int spacing = 15;

            // Walk Points Configuration
            var walkPointsPanel = new Panel
            {
                X = 10,
                Y = yPos,
                Width = 560,
                Height = 120
            };

            walkPointsPanel.AddChild(new Label
            {
                Text = "Path Points (x1,y1;x2,y2;...):",
                ForeColor = Color.FromArgb(200, 100, 200),
                Width = 300,
                Y = 10
            });

            _walkPointsTextBox = new TextBox
            {
                Text = "-0.5,0.5;0.5,0.5;0.5,-0.5",
                Y = 40,
                Width = 560,
                Height = 30,
                PlaceholderText = "Example: -0.5,0.5;0.5,0.5;0.5,-0.5"
            };
            walkPointsPanel.AddChild(_walkPointsTextBox);
            orcWalkTab.AddChild(walkPointsPanel);
            yPos += 130;

            // Walk Speed Control
            var speedSlider = CreateSlider(10, yPos, "Walk Speed", Color.FromArgb(150, 150, 255), 0.1f, 5.0f);
            orcWalkTab.AddChild(speedSlider);
            yPos += panelHeight + spacing;

            // OrcWalk Button
            _orcWalkButton = new Button
            {
                Y = yPos,
                Width = 560,
                Height = 40,
                Text = "Start OrcWalk",
                BackColor = Color.FromArgb(70, 70, 150)
            };
            orcWalkTab.AddChild(_orcWalkButton);

            _tabControl.AddTab(orcWalkTab);
        }

        private Slider CreateSlider(int x, int y, string title, Color color, float minValue, float maxValue)
        {
            return new Slider
            {
                X = x,
                Y = y,
                Width = 560,
                Height = 50, // Altura un poco mayor para mejor manipulación
                Text = title,
                MinValue = minValue,
                MaxValue = maxValue,
                FillColor = color,
                IsIntegerValue = (title != "Walk Speed")
            };
        }

        private void SetupEvents()
        {
            _applyButton.Click += ApplyPlayerChanges;
            _orcWalkButton.Click += ToggleOrcWalk;
        }

        private void ApplyPlayerChanges(Vector2 pos)
        {
            // Actualizar valores en el juego
            if (Process != null)
            {
                Process.Write(PLAYER_SCORE, (int)_scoreSlider.Value);
                Process.Write(PLAYER_HEALTH, _healthSlider.Value);
                Process.Write(PLAYER_LEVEL, (int)_levelSlider.Value);
                Process.WriteString(PLAYER_NAME, _playerNameTextBox.Text);
            }

            ShowStatusMessage("Player changes applied successfully!", Color.FromArgb(0, 255, 0));
        }

        private void ToggleOrcWalk(Vector2 pos)
        {
            _isOrcWalking = !_isOrcWalking;

            if (_isOrcWalking)
            {
                ParseWalkPoints();
                if (_walkPoints.Count == 0)
                {
                    ShowStatusMessage("No valid walk points defined!", Color.FromArgb(255, 100, 100));
                    _isOrcWalking = false;
                    return;
                }

                _currentWalkPoint = 0;
                _lastWalkTime = DateTime.Now;
                _orcWalkButton.Text = "Stop OrcWalk";
                _orcWalkButton.BackColor = Color.FromArgb(150, 70, 70);
                ShowStatusMessage("OrcWalk started!", Color.FromArgb(0, 255, 0));
            }
            else
            {
                _orcWalkButton.Text = "Start OrcWalk";
                _orcWalkButton.BackColor = Color.FromArgb(70, 70, 150);
                ShowStatusMessage("OrcWalk stopped!", Color.FromArgb(200, 200, 200));
            }
        }

        private void ParseWalkPoints()
        {
            _walkPoints.Clear();
            var pointStrings = _walkPointsTextBox.Text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pointStr in pointStrings)
            {
                var coords = pointStr.Split(',');
                if (coords.Length == 2 &&
                    float.TryParse(coords[0], out float x) &&
                    float.TryParse(coords[1], out float y))
                {
                    _walkPoints.Add(new Vector2(x, y));
                }
            }
        }

        private void ShowStatusMessage(string message, Color color)
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = color;
        }

        public void Update()
        {
            if (_isOrcWalking && _walkPoints.Count > 0)
            {
                if ((DateTime.Now - _lastWalkTime).TotalSeconds >= _walkSpeed)
                {
                    MoveToNextPoint();
                    _lastWalkTime = DateTime.Now;
                }
            }
        }

        private void MoveToNextPoint()
        {
            var target = _walkPoints[_currentWalkPoint];
            RemoteFunctionExecutor.CallRemoteFunction(Process.Handle, PLAYER_MOVE_TO, target.X, target.Y);

            _currentWalkPoint = (_currentWalkPoint + 1) % _walkPoints.Count;
        }

        public void Render()
        {
            Update();

            if (InputSystem.GetKeyDown(Keys.F1))
            {
                MainWindow.Visible = !MainWindow.Visible;
            }

            if (_isInitialized && MainWindow != null)
            {
                MainWindow.Update();
                MainWindow.Draw();
            }


            InputSystem.Update();
        }

        public void Shutdown()
        {
            MainWindow?.Close();
            _isInitialized = false;
        }
    }
}