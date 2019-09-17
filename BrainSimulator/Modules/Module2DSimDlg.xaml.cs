﻿//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for Module2DSimDlg.xaml
    /// </summary>
    public partial class Module2DSimDlg : ModuleBaseDlg
    {
        public Module2DSimDlg()
        {
            InitializeComponent();
        }
        //this is here so the last change will cause a screen update after 1 second
        DispatcherTimer dt = null;
        private void Dt_Tick(object sender, EventArgs e)
        {
            dt.Stop(); ;
            Application.Current.Dispatcher.Invoke((Action)delegate { Draw(); });
        }
        public override bool Draw()
        {
            if (!base.Draw())
            {
                if (dt == null)
                {
                    dt = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 100) };
                    dt.Tick += Dt_Tick;
                }
                dt.Stop();
                dt.Start();
                return false;
            }

            Module2DSim parent = (Module2DSim)base.ParentModule;

            //theCanvas.Children.RemoveRange(1, theCanvas.Children.Count-1);
            theCanvas.Children.Clear();
            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;
            if (scale == 0) return false;

            TransformGroup tg = new TransformGroup();
            tg.Children.Add(new RotateTransform(90));
            tg.Children.Add(new ScaleTransform(scale, -scale, 0, 0));// windowCenter.X, windowCenter.Y));
            tg.Children.Add(new TranslateTransform(windowCenter.X, windowCenter.Y));
            theCanvas.RenderTransform = tg;


            //add a background
            Rectangle r = new Rectangle() { Height = parent.boundarySize * 2, Width = parent.boundarySize * 2, Stroke = Brushes.AliceBlue, Fill = Brushes.AliceBlue };
            Canvas.SetLeft(r, -parent.boundarySize);
            Canvas.SetTop(r, -parent.boundarySize);
            theCanvas.Children.Add(r);
            //draw the camera track...
            Polyline p = new Polyline();
            p.StrokeThickness = 1 / scale;
            p.Stroke = Brushes.Pink;
            for (int i = 0; i < parent.CameraTrack.Count; i++)
            {
                p.Points.Add(
                    new Point(
                        parent.CameraTrack[i].X,
                        parent.CameraTrack[i].Y
                        )
                        );
            }
            theCanvas.Children.Add(p);

            //draw the objects
            for (int i = 0; i < parent.objects.Count; i++)
            {
                theCanvas.Children.Add(new Line
                {
                    X1 = parent.objects[i].P1.X,
                    X2 = parent.objects[i].P2.X,
                    Y1 = parent.objects[i].P1.Y,
                    Y2 = parent.objects[i].P2.Y,
                    StrokeThickness = 5 / scale,
                    Stroke = new SolidColorBrush(parent.objects[i].theColor)
                });
            }

            //draw the antennae...
            if (parent.antennaeActual.Length == 2)
            {
                for (int i = 0; i < parent.antennaeActual.Length; i++)
                    theCanvas.Children.Add(new Line
                    {
                        X1 = parent.CameraPosition.X,
                        Y1 = parent.CameraPosition.Y,
                        X2 = parent.antennaeActual[i].X,
                        Y2 = parent.antennaeActual[i].Y,
                        StrokeThickness = 2 / scale,
                        Stroke = Brushes.Black
                    });
            }

            //draw the current field of view
            for (int i = 0; i < parent.currentView0.Count; i++)
            {
                theCanvas.Children.Add(new Line
                {
                    X1 = parent.currentView0[i].P1.X,
                    X2 = 1 / scale + parent.currentView0[i].P1.X,
                    Y1 = parent.currentView0[i].P1.Y,
                    Y2 = 1 / scale + parent.currentView0[i].P1.Y,
                    StrokeThickness = 3 / scale,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    Stroke = new SolidColorBrush(parent.currentView0[i].theColor)
                });
            }
            for (int i = 0; i < parent.currentView1.Count; i++)
            {
                theCanvas.Children.Add(new Line
                {
                    X1 = parent.currentView1[i].P1.X,
                    X2 = 1 / scale + parent.currentView1[i].P1.X,
                    Y1 = parent.currentView1[i].P1.Y,
                    Y2 = 1 / scale + parent.currentView1[i].P1.Y,
                    StrokeThickness = 3 / scale,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    Stroke = new SolidColorBrush(parent.currentView1[i].theColor)
                });
            }

            return true;
        }


        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw();
        }


        private void TheCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Module2DSim parent = (Module2DSim)base.ParentModule;

            Point windowSize = new Point(theCanvas.ActualWidth, theCanvas.ActualHeight);
            Point windowCenter = new Point(windowSize.X / 2, windowSize.Y / 2);
            float scale = (float)Math.Min(windowSize.X, windowSize.Y) / 12;


            Point position = e.GetPosition(theCanvas);

            PointPlus v = new PointPlus { P = (Point)(position - parent.CameraPosition) };
            float dist = (float)v.R;
            double angle = (float)v.Theta;
            double deltaAngle = angle - parent.CameraDirection1;
            Module naGoToDest = MainWindow.theNeuronArray.FindAreaByLabel("ModuleGoToDest");
            if (naGoToDest != null)
            {
                naGoToDest.GetNeuronAt("Go").SetValue(1);
                naGoToDest.GetNeuronAt("Theta").SetValue((float)deltaAngle);
                naGoToDest.GetNeuronAt("R").SetValue(dist);
            }
            else
            {
                Module naBehavior = MainWindow.theNeuronArray.FindAreaByLabel("ModuleBehavior");
                if (naBehavior != null)
                {
                    naBehavior.GetNeuronAt("TurnTo").SetValue(1);
                    naBehavior.GetNeuronAt("Theta").SetValue((float)-deltaAngle);
                    naBehavior.GetNeuronAt("MoveTo").SetValue(1);
                    naBehavior.GetNeuronAt("R").SetValue(dist);
                }
            }
        }

        private void TheCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Module2DSim parent = (Module2DSim)base.ParentModule;
            parent.SetModel();
        }
    }
}