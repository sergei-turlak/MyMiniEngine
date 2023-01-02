using MyEngine.Game;
using System.Windows.Forms;

namespace MyMiniEngine
{
    public partial class MainForm : Form
    {
        private Game game;
        
        public MainForm()
        {
            InitializeComponent();
            game = new Game(outputBox, this);
            DeleteLabel();
            game.RunGame();
        }

        private void DeleteLabel()
        {
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 5000;
            timer.Tick += (s, e) =>
            {
                Controls.Remove(label);
                label.Dispose();
                timer.Stop();
            };
            timer.Start();
        }
    }
}