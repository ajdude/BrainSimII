﻿//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

namespace BrainSimulator
{
    public class Segment
    {
        public PointPlus P1;
        public PointPlus P2;
        public Color theColor;
        public PointPlus MidPoint()
        {
            return new PointPlus { X = (P1.X + P2.X) / 2, Y = (P1.Y + P2.Y) / 2 };
        }
    }



    public static class Utils
    {
        public static Color FromArgb(int theColor)
        {
            Color c = new Color();
            c.A = 255;
            c.B = (byte)(theColor & 0xff);
            c.G = (byte)(theColor >> 8 & 0xff);
            c.R = (byte)(theColor >> 16 & 0xff);
            return c;
        }
        public static int ToArgb(Color theColor)
        {
            int retVal = 0;
            //retVal += theColor.A << 24; ??
            retVal += theColor.R << 16;
            retVal += theColor.G << 8;
            retVal += theColor.B;
            return retVal;
        }

        public static bool Close(float f1, float f2, float toler = 0.2f)
        {
            float dif = f2 - f1;
            dif = Math.Abs(dif);
            if (dif > toler) return false;
            return true;
        }

        public static bool Close(int a, int b)
        {
            if (Math.Abs(a - b) < 4) return true;
            return false;
        }
        public static bool ColorClose(Color c1, Color c2)
        {
            if (Close(c1.R, c2.R) && Close(c1.G, c2.G) && Close(c1.B, c2.B)) return true;
            return false;
        }

        public static string GetColorName(Color col)
        {
            PropertyInfo[] p1 = typeof(Colors).GetProperties();
            foreach (PropertyInfo p in p1)
            {
                Color c = (Color)p.GetValue(null);
                if (ColorClose(c, col))
                    return p.Name;
            }
            return "??";
        }

        // Calculate the distance between
        // point pt and the segment p1 --> p2.
        public static double FindDistanceToSegment(
            Point pt, Point p1, Point p2, out Point closest)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            if ((dx == 0) && (dy == 0))
            {
                // It's a point not a line segment.
                closest = p1;
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            double t = ((pt.X - p1.X) * dx + (pt.Y - p1.Y) * dy) /
                (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                closest = new Point(p1.X, p1.Y);
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
            }
            else if (t > 1)
            {
                closest = new Point(p2.X, p2.Y);
                dx = pt.X - p2.X;
                dy = pt.Y - p2.Y;
            }
            else
            {
                closest = new Point(p1.X + t * dx, p1.Y + t * dy);
                dx = pt.X - closest.X;
                dy = pt.Y - closest.Y;
            }

            return Math.Sqrt(dx * dx + dy * dy);
        }


        // Find the point of intersection between
        // the lines p1 --> p2 and p3 --> p4.
        public static void FindIntersection(
            Point p1, Point p2, Point p3, Point p4,
            out bool lines_intersect, out bool segments_intersect,
            out Point intersection,
            out Point close_p1, out Point close_p2,
            out double collisionAngle)
        {
            // Get the segments' parameters.
            double dx12 = p2.X - p1.X;
            double dy12 = p2.Y - p1.Y;
            double dx34 = p4.X - p3.X;
            double dy34 = p4.Y - p3.Y;

            double theta1 = Math.Atan2(dy12, dx12); //obstacle
            double theta2 = Math.Atan2(dy34, dx34); //motion attempt
            collisionAngle = theta2 - theta1;

            // Solve for t1 and t2
            double denominator = (dy12 * dx34 - dx12 * dy34);

            double t1 =
                ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34)
                    / denominator;
            if (double.IsInfinity(t1))
            {
                // The lines are parallel (or close enough to it).
                lines_intersect = false;
                segments_intersect = false;
                intersection = new Point(float.NaN, float.NaN);
                close_p1 = new Point(float.NaN, float.NaN);
                close_p2 = new Point(float.NaN, float.NaN);
                return;
            }
            lines_intersect = true;

            double t2 =
                ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12)
                    / -denominator;

            // Find the point of intersection.
            intersection = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            segments_intersect =
                ((t1 >= 0) && (t1 <= 1) &&
                 (t2 >= 0) && (t2 <= 1));

            // Find the closest points on the segments.
            if (t1 < 0)
            {
                t1 = 0;
            }
            else if (t1 > 1)
            {
                t1 = 1;
            }

            if (t2 < 0)
            {
                t2 = 0;
            }
            else if (t2 > 1)
            {
                t2 = 1;
            }

            close_p1 = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);
            close_p2 = new Point(p3.X + dx34 * t2, p3.Y + dy34 * t2);
        }

        public static float DistancePointToLine(Point P, Point P1, Point P2)
        {
            double distance = Math.Abs((P2.X - P1.X) * (P1.Y - P.Y) - (P1.X - P.X) * (P2.Y - P1.Y)) /
                    Math.Sqrt(Math.Pow(P2.X - P1.X, 2) + Math.Pow(P2.Y - P1.Y, 2));
            return (float)distance;
        }

        //find a point which is dist off the end of a line segment
        public static PointPlus ExtendSegment(Point P1, Point P2, float dist, bool firstPt)
        {
            if (firstPt)
            {
                Vector v = P2 - P1;
                double changeLength = (v.Length + dist) / v.Length;
                v = Vector.Multiply(changeLength, v);
                PointPlus newPoint = new PointPlus { P = P2 - v };
                return newPoint;
            }
            else
            {
                Vector v = P1 - P2;
                double changeLength = (v.Length + dist) / v.Length;
                v = Vector.Multiply(changeLength, v);
                PointPlus newPoint = new PointPlus { P = P1 - v };
                return newPoint;
            }
        }
    }


}