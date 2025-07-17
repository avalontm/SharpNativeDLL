using AvalonInjectLib;
using static AvalonInjectLib.Structs;

namespace TargetGUI
{
    public partial class Form1 : Form
    {
        const int PLAYER_MOVE_TO = 0x3F1220;

        private List<Vector2> _walkPoints = new List<Vector2>();
        ProcessEntry Process;
        private int _currentWalkPoint = 0;
        private bool _isOrcWalking = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Process = ProcessManager.Create("Target.exe");

            if (Process == null)
            {
                MessageBox.Show("El proceso Target no esta abierto.", "Error");
                Application.Exit();
                return;
            }

            LblProcessId.Text = $"Process: {Process.ProcessId:X8}";
        }

        private void BntWalk_Click(object sender, EventArgs e)
        {
            ParseWalkPoints();
            _isOrcWalking = !_isOrcWalking;

            if (_isOrcWalking)
            {
                BtnWalk.Text = "Detener";
            }
            else
            {
                BtnWalk.Text = "Iniciar";
            }
        }

        private void ParseWalkPoints()
        {
            _walkPoints.Clear();
            var pointStrings = txtWalk.Text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

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

        private void MoveToNextPoint()
        {
            var target = _walkPoints[_currentWalkPoint];
            RemoteFunctionExecutor.CallRemoteFunction(Process.Handle, PLAYER_MOVE_TO, target.X, target.Y);

            _currentWalkPoint = (_currentWalkPoint + 1) % _walkPoints.Count;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_isOrcWalking && _walkPoints.Count > 0)
            {
                MoveToNextPoint();
            }
        }
    }
}
