using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyMiniEngine.Engine
{
    internal delegate void OnStatisticsChanged(string stats);

    internal class Engine
    {
        public event OnStatisticsChanged OnStatisticsChanged;

        public List<SphereModel> Spheres { get; set; }
        public Camera Camera { get; }
        public Color[,] Background { get; }
        public PictureBox OutputBox { get; }

        private bool renderingThreadEnabled; /// do not set value, instead use methods run/stop rendering thread

        public Engine(Camera camera, Bitmap background, PictureBox outputBox, OnStatisticsChanged statisticsHandler)
        {
            Spheres = new List<SphereModel>();
            Camera = camera;
            Background = new Color[Camera.ResolHeight, Camera.ResolWidth];
            for (int i = 0; i < Camera.ResolHeight; i++)
                for (int j = 0; j < Camera.ResolWidth; j++)
                    Background[i, j] = background.GetPixel(j, i);

            OutputBox = outputBox;
            OnStatisticsChanged += statisticsHandler;
        }

        public SphereModel SpawnSphere(Point3d position, double radius, Rgb color)
        {
            SphereModel sphere = new SphereModel(position, radius, color);
            Spheres.Add(sphere);
            return sphere;
        }

        public void DeleteSphere(SphereModel sphere)
        {
            Spheres.Remove(sphere);
        }

        public void RunMovingSphere(SphereModel sphere, Point3d endPoint, int time, Predicate<Point3d> cancellationCondition = null, Action afterArriving = null)
        {
            int timeInGroupsOf15Millis = time / 15;
            Point3d startPoint = sphere.Center;
            double
                xStepLength = (endPoint.x - startPoint.x) / timeInGroupsOf15Millis,
                yStepLength = (endPoint.y - startPoint.y) / timeInGroupsOf15Millis,
                zStepLength = (endPoint.z - startPoint.z) / timeInGroupsOf15Millis;
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 15; //one tick takes ~15 milliseconds
            int i = 0;
            timer.Tick += (s, e) =>
            {
                sphere.Center = new Point3d(sphere.Center.x + xStepLength, sphere.Center.y + yStepLength, sphere.Center.z + zStepLength);
                i++;
                if (i >= timeInGroupsOf15Millis || (cancellationCondition != null && cancellationCondition(sphere.Center)))
                {
                    timer.Stop();
                    if (afterArriving != null) afterArriving();
                }
            };
            timer.Start();
        }

        public void RunAwaitingCollision(SphereModel s1, SphereModel s2, Action<SphereModel, SphereModel> onSpheresCollision, long maxAwaitingTime)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 15;
            timer.Tick += (s, e) =>
            {
                if (s1.Center.GetDistance(s2.Center) <= s1.Radius + s2.Radius)
                {
                    timer.Stop();
                    stopwatch.Stop();
                    onSpheresCollision(s1, s2);
                }
                if (stopwatch.ElapsedMilliseconds > maxAwaitingTime)
                {
                    timer.Stop();
                    stopwatch.Stop();
                }
            };
            timer.Start();
        }

        public void RunResizingSphere(SphereModel sphere, double newRadius, int time, Action afterResizing = null)
        {
            int timeInGroupsOf15Millis = time / 15;
            double resizingStep = (newRadius - sphere.Radius) / timeInGroupsOf15Millis;
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 15;
            int i = 0;
            timer.Tick += (s, e) =>
            {
                sphere.Radius += resizingStep;
                i++;
                if (i >= timeInGroupsOf15Millis)
                {
                    timer.Stop();
                    if (afterResizing != null) afterResizing();
                }
            };
            timer.Start();
        }

        public void RunConstantRedrawing(SphereModel sphere)
        {
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 15;
            timer.Tick += (s, e) =>
            {
                byte r = sphere.GeneralColor.r, g = sphere.GeneralColor.g, b = sphere.GeneralColor.b;
                if (r < 255) r += 3;
                else if (g < 255) g += 3;
                else if (b < 255) b += 3;
                else r = g = b = 70;
                sphere.GeneralColor = new Rgb(r, g, b);
            };
            timer.Start();
        }

        public void Render()
        {
            //measure elapsed time 
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //capturing the state of sphere collection because List Spheres is shared resource than can be read and set from different threads at the same time
            SphereModel[] spheres = new SphereModel[Spheres.Count];
            Spheres.CopyTo(spheres);

            Color[,] outputColors = new Color[Camera.ResolHeight, Camera.ResolWidth];
            for (int i = 0; i < Camera.ResolHeight; i++)
                for (int j = 0; j < Camera.ResolWidth; j++)
                    outputColors[i, j] = Background[i, j]; //default settings for output

            //taking every pixel
            Parallel.For(0, Camera.ResolHeight, (i) =>
            {
                double AB, AC, CB, height;
                for (int j = 0; j < Camera.ResolWidth; j++)
                {
                    for (int l = 0; l < spheres.Length; l++) //taking every model 
                    {
                        //constructing the triangle ABC from points: A - in alpha grid B - in omega grid C - some sphere's center
                        AB = Camera.CameraDistancesToGrid[i, j];
                        AC = Camera.Coords.GetDistance(spheres[l].Center);
                        CB = spheres[l].Center.GetDistance(Camera.Grid[i, j]);
                        height = MathOperations.GetTriangleHeight(AB, AC, CB);
                        if (double.IsNaN(height) || double.IsInfinity(height)) break;

                        //if current sphere body is located between points at alpha and omega grids (if C + radius is between A and B)
                        if (height <= spheres[l].Radius && AC < AB && CB < AB)
                        {
                            ///////////// calculating the blackness for R G and B //////////////
                            //displacing center for darking specified corner
                            double ySunCoef = (Camera.Grid[i, j].y - Camera.Coords.y) / (Camera.ResolWidth * Camera.Frequency / 2);
                            if (ySunCoef < -1.0 || ySunCoef > 1.0) break;
                            double zSunCoef = (Camera.Grid[i, j].z - Camera.Coords.z - Camera.ResolHeight * Camera.Frequency / 2) / (Camera.ResolHeight * Camera.Frequency);
                            if (zSunCoef < -1.0 || zSunCoef > 0) break;
                            //this coefs must be form - 1.0 to + 1.0

                            Point3d displacedCenter = new Point3d(spheres[l].Center.x, spheres[l].Center.y - spheres[l].Radius * ySunCoef, spheres[l].Center.z - spheres[l].Radius * zSunCoef); //spheres[l].Radius must be multiplied on coef from -1.0 to +1.0
                            AC = Camera.Coords.GetDistance(displacedCenter);
                            CB = displacedCenter.GetDistance(Camera.Grid[i, j]);
                            height = MathOperations.GetTriangleHeight(AB, AC, CB);
                            if (double.IsNaN(height) || double.IsInfinity(height)) break;

                            //calculating dark level by average between distance cam to sphere and distance surface to displaced center
                            double darkLevel = (AC / AB + height / spheres[l].Radius) / 2; // this level may be from 0.0 to 1.0  (unfortunately height may be bigger than spheres[l].Radius, so instead 1.0 proportion may be even 1.77, but i need it for the beauty. Next line we see (if darklevel > 1)....)
                            if (darkLevel > 1) darkLevel = 1;

                            Rgb source = spheres[l].GeneralColor;
                            outputColors[i, j] = Color.FromArgb(
                                (int)(source.r - source.r * darkLevel),
                                (int)(source.g - source.g * darkLevel),
                                (int)(source.b - source.b * darkLevel));
                            break;
                        }
                    }
                }
            });

            //drawing output
            GC.Collect(2, GCCollectionMode.Forced); //calling finalizers for previous obsolete bitmaps
            GC.WaitForPendingFinalizers();
            Bitmap outputBitmap = new Bitmap(Camera.ResolWidth, Camera.ResolHeight);
            for (int i = 0; i < Camera.ResolHeight; i++)
                for (int j = 0; j < Camera.ResolWidth; j++)
                    outputBitmap.SetPixel(j, i, outputColors[i, j]);
            OutputBox.Invoke(new Action(() => OutputBox.Image = outputBitmap));

            //display statistics
            stopwatch.Stop();
            OnStatisticsChanged($"{(1000.0d / stopwatch.ElapsedMilliseconds).ToString(".")} FPS | {Camera.ResolWidth} x {Camera.ResolHeight}");
        }

        private void RunRendering()
        {
            if (!renderingThreadEnabled) return;
            Render();
            RunRendering();
        }

        public void RunRenderingThread()
        {
            if (renderingThreadEnabled) throw new Exception("Rendering thread is already working: boolean renderingThreadEnabled is true");
            renderingThreadEnabled = true;
            Thread thread = new Thread(() => RunRendering());
            thread.Priority = ThreadPriority.AboveNormal;
            thread.IsBackground = true;
            thread.Start();
        }

        public void FinishRenderingThread()
        {
            renderingThreadEnabled = false;
        }
    }
}
