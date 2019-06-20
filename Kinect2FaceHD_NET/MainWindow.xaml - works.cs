using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Diagnostics;

using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.FaceTracking;
using System.IO;

namespace Kinect2FaceHD_NET
{
    public partial class MainWindow : Window
    {
        private KinectSensor _sensor = null;

        private BodyFrameSource _bodySource = null;

        private BodyFrameReader _bodyReader = null;

        private HighDefinitionFaceFrameSource _faceSource = null;

        private HighDefinitionFaceFrameReader _faceReader = null;

        private FaceAlignment _faceAlignment = null;

        //    private IReadOnlyDictionary<FaceShapeAnimations, float> AnimationUnits { get; }

        private FaceModel _faceModel = null;

        private List<Ellipse> _points = new List<Ellipse>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _bodySource = _sensor.BodyFrameSource;
                _bodyReader = _bodySource.OpenReader();
                _bodyReader.FrameArrived += BodyReader_FrameArrived;

                _faceSource = new HighDefinitionFaceFrameSource(_sensor);

                _faceReader = _faceSource.OpenReader();
                _faceReader.FrameArrived += FaceReader_FrameArrived;

                _faceModel = new FaceModel();
                _faceAlignment = new FaceAlignment();


                _sensor.Open();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_faceModel != null)
            {
                _faceModel.Dispose();
                _faceModel = null;
            }

            GC.SuppressFinalize(this);
        }

        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    Body[] bodies = new Body[frame.BodyCount];
                    frame.GetAndRefreshBodyData(bodies);

                    Body body = bodies.Where(b => b.IsTracked).FirstOrDefault();

                    if (!_faceSource.IsTrackingIdValid)
                    {
                        if (body != null)
                        {
                            _faceSource.TrackingId = body.TrackingId;
                        }
                    }
                }
            }
        }

        private void FaceReader_FrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null && frame.IsFaceTracked)
                {
                    frame.GetAndRefreshFaceAlignmentResult(_faceAlignment);
                    UpdateFacePoints();

                    //    float jaw = (float)FaceShapeAnimations.JawOpen;
                    ActionUnits();
                }
            }
        }

        private void UpdateFacePoints()
        {
            if (_faceModel == null) return;

            var vertices = _faceModel.CalculateVerticesForAlignment(_faceAlignment);

            if (vertices.Count > 0)
            {
                if (_points.Count == 0)
                {
                    for (int index = 0; index < vertices.Count; index++)
                    {
                        Ellipse ellipse = new Ellipse
                        {
                            Width = 2.0,
                            Height = 2.0,
                            Fill = new SolidColorBrush(Colors.Blue)
                        };

                        _points.Add(ellipse);
                    }

                    foreach (Ellipse ellipse in _points)
                    {
                        canvas.Children.Add(ellipse);
                    }
                }

                for (int index = 0; index < vertices.Count; index++)
                {
                    CameraSpacePoint vertice = vertices[index];
                    DepthSpacePoint point = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(vertice);
                    //inner eyebrow check
                    if (index == 803 || index == 346)
                    {
                        DepthSpacePoint innerbrowl = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(vertices[803]);
                        DepthSpacePoint innerbrowr = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(vertices[346]);
                        double dx = innerbrowl.X - innerbrowr.X;
                        double dy = innerbrowl.Y - innerbrowr.Y;
                        double dist = dx * dx + dy * dy;
                        //  Debug.WriteLine("eyebrowdistance " + Math.Round(Math.Sqrt( dist), 1));           
                    }
                    //eyelid check
                    if (index == 866 || index == 868)
                    {
                        DepthSpacePoint eyelid1 = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(vertices[866]);
                        DepthSpacePoint eyelid2 = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(vertices[868]);
                        double dx = eyelid1.X - eyelid2.X;
                        double dy = eyelid1.Y - eyelid2.Y;
                        double dist = dx * dx + dy * dy;
                        //  Debug.WriteLine("eyelidopen " + Math.Round(Math.Sqrt(dist), 1));
                    }
                    //nose wrinkler
                    if (index == 673 || index == 24)
                    {
                        DepthSpacePoint nose1 = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(vertices[11]);
                        DepthSpacePoint nose2 = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(vertices[1]);
                        double dx = nose1.X - nose2.X;
                        double dy = nose1.Y - nose2.Y;
                        double dist = dx * dx + dy * dy;
                        //    Debug.WriteLine("side nose distance " + Math.Round(Math.Sqrt(dist), 1));
                    }
                    //lip level raise
                    if (index == 309 || index == 761)
                    {
                        DepthSpacePoint nose1 = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(vertices[309]);
                        DepthSpacePoint nose2 = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(vertices[761]);
                        double dx = nose1.X - nose2.X;
                        double dy = nose1.Y - nose2.Y;
                        double dist = dx * dx + dy * dy;
                        //    Debug.WriteLine("lip level raise " + Math.Round(Math.Sqrt(dist), 1));
                    }
                    if (float.IsInfinity(point.X) || float.IsInfinity(point.Y)) return;

                    Ellipse ellipse = _points[index];

                    Canvas.SetLeft(ellipse, point.X);
                    Canvas.SetTop(ellipse, point.Y);

                }
            }
        }

        private void ActionUnits()
        {
            float JawOpen0 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)0];
            float LipStretcherRight3 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)3];
            float LipStretcherLeft4 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)4];
            float LipCornerPullerLeft5 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)5];
            float LipCornerPullerRight6 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)6];
            float LipCornerDepressorLeft7 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)7];
            float LipCornerDepressorRight8 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)8];
            float LeftcheekPuff9 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)9];
            float RightcheekPuff10 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)10];
            float LefteyeClosed11 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)11];
            float RighteyeClosed12 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)12];
            float RighteyebrowLowerer13 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)13];
            float LefteyebrowLowerer14 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)14];
            float LowerlipDepressorLeft15 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)15];
            float LowerlipDepressorRight16 = _faceAlignment.AnimationUnits[(FaceShapeAnimations)16];

            // added by ALA 8/11/2017
            //String ResultsFileName = "c:\\Users\\Public\\ResultsForAndrea.txt";
            String ResultsFileName = "c:\\Users\\abbott\\Documents\\Kinect Studio\\ResultsForAndrea.txt";
            DateTime now = DateTime.Now;

            // Start

            if ((RighteyebrowLowerer13 > 0.01 || LefteyebrowLowerer14 > 0.01))
            {
                //sad, fear and angry
                if ((LipCornerDepressorLeft7 < 0.3 && LipCornerDepressorRight8 < 0.3))
                {
                    // Debug.WriteLine("Sad");

                    // changed by ALA 8/14/2017 (also other cases below)
                    //DateTime now = DateTime.Now;
                    now = DateTime.Now;

                    using (FileStream fs = new FileStream(ResultsFileName, FileMode.Append, FileAccess.Write))
                    //using (FileStream fs = new FileStream(@"c:\Results.txt", FileMode.Append, FileAccess.Write))
                    //using (FileStream fs = new FileStream(@"e:\Results.txt", FileMode.Append, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        // changed by ALA 8/14/2017 (also other cases below)
                        //sw.WriteLine("Sad", now);
                        sw.WriteLine("Sad" + "\t" + now);
                    }
                }
                else if ((LefteyeClosed11 > 0.35 && RighteyeClosed12 > 0.35))
                {
                    //Debug.WriteLine("Angry");
                    now = DateTime.Now;

                    using (FileStream fs = new FileStream(ResultsFileName, FileMode.Append, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("Angry" + "\t" + now);
                    }
                }
                else if ((JawOpen0 < 0.1))
                {
                    now = DateTime.Now;

                    //Debug.WriteLine("Neutral");
                    using (FileStream fs = new FileStream(ResultsFileName, FileMode.Append, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("Neutral" + "\t" + now);
                    }
                }
                else
                {
                    //Debug.WriteLine("Fear");
                    now = DateTime.Now;

                    using (FileStream fs = new FileStream(ResultsFileName, FileMode.Append, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("Fear" + "\t" + now);
                    }
                }
            }
            //happy surprise and disgust
            else if ((JawOpen0 > 0.4))
            {
                //Debug.WriteLine("Surprise");
                now = DateTime.Now;

                using (FileStream fs = new FileStream(ResultsFileName, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine("Surprise" + "\t" + now);
                }
            }
            else if ((JawOpen0 < 0.4))
            {
                if (LipCornerPullerLeft5 > 0.3 || LipCornerPullerLeft5 > 0.3)
                {
                    //Debug.WriteLine("Happy");
                    now = DateTime.Now;

                    using (FileStream fs = new FileStream(ResultsFileName, FileMode.Append, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("Happy" + "\t" + now);
                    }
                }
                else if (LipCornerPullerLeft5 >= 0.1 && LipCornerPullerLeft5 >= 0.1)
                {
                    // Debug.WriteLine("Disgust");
                    now = DateTime.Now;

                    using (FileStream fs = new FileStream(ResultsFileName, FileMode.Append, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("Disgust" + "\t" + now);
                    }
                }
                else
                {
                    //Debug.WriteLine("neutral");
                    now = DateTime.Now;

                    using (FileStream fs = new FileStream(ResultsFileName, FileMode.Append, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("neutral" + "\t" + now);
                    }
                }
            }


            //taking values
            // if (JawOpen0 > 0.3 )
            //{
            //    Debug.WriteLine("0");
            //    Debug.WriteLine(JawOpen0);
            //   Debug.WriteLine(LipCornerPullerRight6);
            //   Debug.WriteLine("15 and 16");
            //   Debug.WriteLine(LowerlipDepressorLeft15);
            //   Debug.WriteLine(LowerlipDepressorRight16);
            //}

        }
    }
}