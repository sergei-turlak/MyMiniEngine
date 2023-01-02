using MyMiniEngine.Engine;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MyEngine.Game
{
    internal class Game
    {
        private Engine engine;
        private SphereModel mainSphere;
        private Random rand;

        public Game(PictureBox outputBox, Form parentForm)
        {
            Camera camera = new Camera(new Point3d(0, 0, 0), new Point3d(10_000, 0, 0), 480, 272, 20);
            OnStatisticsChanged handler = (stats) => parentForm.Invoke(new Action(() => parentForm.Text = stats));
            Bitmap background = new Bitmap(camera.ResolWidth, camera.ResolHeight);
            using (Graphics g = Graphics.FromImage(background))
                g.DrawImage(MyMiniEngine.Properties.Resources.Background, 0, 0, camera.ResolWidth, camera.ResolHeight);
            engine = new Engine(camera, background, outputBox, handler);
            rand = new Random(Guid.NewGuid().GetHashCode());
        }

        public void RunGame()
        {
            mainSphere = engine.SpawnSphere
                (new Point3d(500, 0, 0), 30, new Rgb((byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255)));
            engine.RunConstantRedrawing(mainSphere);

            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 3000;
            timer.Tick += (s, e) => RunNewSphere();
            timer.Start();

            engine.OutputBox.MouseMove += (s, e) => MoveMainSphere(e);
            engine.OutputBox.MouseEnter += (s, e) => Cursor.Hide();
            engine.OutputBox.MouseLeave += (s, e) => Cursor.Show();

            RunArrangingSpheresInOrder();

            engine.RunRenderingThread();
        }

        private void RunNewSphere()
        {
            SphereModel newSphere = engine.SpawnSphere
                    (engine.Camera.Grid[rand.Next(60, engine.Camera.ResolHeight) - 60, rand.Next(60, engine.Camera.ResolWidth - 60)],
                    rand.Next(20, 40),
                    new Rgb((byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255)));
            engine.RunMovingSphere
                (newSphere, engine.Camera.Coords, 6000,
                (point) => point.x < mainSphere.Center.x ? true : false,
                () => engine.RunMovingSphere
                (newSphere, new Point3d(-50, newSphere.Center.y, newSphere.Center.z), 1000, null,
                () => engine.DeleteSphere(newSphere)));
            engine.RunAwaitingCollision(mainSphere, newSphere, OnSpheresCollision, 15000);
        }

        private void OnSpheresCollision(SphereModel s1, SphereModel s2)
        {
            engine.DeleteSphere(s2);
            s1.GeneralColor = s2.GeneralColor;
            double oldRad = s1.Radius;
            engine.RunResizingSphere
                (s1, oldRad * 2, 300,
                () => engine.RunResizingSphere(s1, oldRad / 2, 300,
                () => engine.RunResizingSphere(s1, oldRad, 150)));
        }

        private void MoveMainSphere(MouseEventArgs e)
        {
            double x = e.X * ((double)engine.Camera.ResolWidth / (double)engine.OutputBox.Width);
            double y = e.Y * ((double)engine.Camera.ResolHeight / (double)engine.OutputBox.Height);
            if (x == engine.Camera.ResolWidth) x = engine.Camera.ResolWidth - 1;
            if (y == engine.Camera.ResolHeight) y = engine.Camera.ResolHeight - 1;
            double coef = mainSphere.Center.x / engine.Camera.DirectionVector.x;

            mainSphere.Center = new Point3d(mainSphere.Center.x, engine.Camera.Grid[(int)(y), (int)(x)].y * coef, engine.Camera.Grid[(int)(y), (int)(x)].z * coef);
        }

        private void RunArrangingSpheresInOrder()
        {
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 100;
            timer.Tick += (s, e) =>
            {
                engine.Spheres.Sort((s1, s2) => s1.Center.x > s2.Center.x ? 1 : -1);
            };
            timer.Start();
        }
    }
}
