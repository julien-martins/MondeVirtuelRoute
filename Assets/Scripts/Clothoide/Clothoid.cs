using System;
using System.Collections.Generic;
using UnityEngine;

namespace Curves
{
    /// <summary>
    /// A clothoid curve. See https://en.wikipedia.org/wiki/Euler_spiral for details.
    /// </summary>
    [Serializable]
    public class Clothoid : Curve
    {
        /// <summary>
        /// Clothoid parameter A.
        /// </summary>
        private readonly double _a;

        public double A
        {
            get
            {
                return _a;
            }
        }

        private bool _generatePointList;

        /// <summary>
        /// Getting and setting the number of iterations during integration of a point.
        /// </summary>
        public int NumIterations { get; set; } = 12;




        /// <summary>
        /// Constructor of the clothoid curve.
        /// </summary>
        /// <param name="startX">X coordinate the clothoid starts at.</param>
        /// <param name="startY">X coordinate the clothoid starts at.</param>
        /// <param name="startDirection">Angle the clothoid starts at in radians.</param>
        /// <param name="startCurvature">Curvature the clothoid starts with.</param>
        /// <param name="a">Clothoid parameter A.</param>
        /// <param name="length">Length of the clothoid.</param>
        public Clothoid(double startX, double startY, double startDirection, double startCurvature, double a, double length)
        {
            _start_x = startX;
            _start_y = startY;
            _start_direction = startDirection;
            _start_curvature = startCurvature;
            _a = a;
            _length = length;

            Posture2D endPosture = InterpolatePosture2D(1.0);

            _end_x = endPosture.X;
            _end_y = endPosture.Y;
            _end_direction = endPosture.Direction;
            _end_curvature = endPosture.Curvature;
        }

        public static Clothoid FromPoseAndPoint( double x0, double y0, double direction0, double xf, double yf )
        {
            // transform point pf into local coordinate system of the clothoid:
            Point2D p = CoordinateSystemConverter.ToPoseCoordinateSystem(x0, y0, direction0, xf, yf);

            // p.X is behind the origing of the clothoid, no solution: return null
            if (p.X < 0) {  return null; }

            double tauMax = Angle.DegreeToRadians(240.0f);
            double AUnit = 1.0f;
            double SUnit = Math.Sqrt(tauMax * 2.0f * AUnit);

            Clothoid UnitC = new Clothoid(0, 0, 0, 0, AUnit, SUnit);
            Point2D UnitEndPos = UnitC.InterpolatePoint2D(1);
            int vSide = Geometry.PointOnSideOfLine(new Point2D(0, 0), UnitEndPos, p);
            // Point is on the left => not in the valid set (angle bigger than 240 Deg)
            if (vSide == 1)
            {
                return null;
            }

            bool switchedASign = false;

            if (p.Y < 0) // make p.Y positive for sake of simplicity and change sign at end again
            {
                switchedASign = true;
                p.Y *= -1.0f;
            }
            else if (Math.Abs(p.Y) < 0.001f) // smaller than a millimeter: it's a line
            {
                double pA = double.MaxValue;
                double pLength = p.X;
                return new Clothoid(x0, y0, direction0, 0, pA, pLength);
            }

            // calculate ratio
            double gs_Pf = Math.Sqrt(p.X * p.X + p.Y * p.Y);

            double distanceToLine = double.MaxValue;
            double binSearch_SMin = 0;
            double binSearch_SMid = 0;
            double binSearch_SMax = SUnit;


            // Stats
            int numberOfIterations = 0;

            // Binary Search
            while (distanceToLine > 0.000001f)
            {
                binSearch_SMid = (binSearch_SMin + binSearch_SMax) / 2.0f;
                Clothoid estC = new Clothoid(0, 0, 0, 0, AUnit, binSearch_SMid);
                Point2D lCEnd = estC.InterpolatePoint2D(1);

                int side = Geometry.PointOnSideOfLine(new Point2D(0, 0), p, lCEnd);
                if (side == 1)  // left of line => s has to shrink
                {
                    binSearch_SMax = binSearch_SMid;
                }
                else if (side == -1) // right of line => s has to grow
                {
                    binSearch_SMin = binSearch_SMid;

                }
                else if (side == 0) // point is on line
                {
                    break;
                }
                // distance between the endpoint of the clothoid to the line to point p
                distanceToLine = Geometry.DistancePointToLine(new Point2D(0, 0), p, lCEnd);
                numberOfIterations++;

                if (numberOfIterations > 100)
                {
                    break;
                }
            }

            
            double sUnit = binSearch_SMid;
            // Calculate Tau
            double tau = (sUnit * sUnit) / (2.0); // A = 1

            Clothoid clothoidA1 = new Clothoid(0, 0, 0, 0, AUnit, binSearch_SMid);

            Point2D PA = clothoidA1.InterpolatePoint2D(1);
            double gs_PA = Point2D.DistanceToOrigin(PA);

            double sA = binSearch_SMid;

            // calculate sf
            double sf = (sA / gs_PA) * gs_Pf;

            // calculate A
            double Af = Math.Sqrt((sf * sf) / (2.0 * tau));

            if (switchedASign)
            {
                Af *= -1.0;
            }

            double length = sf;
            Clothoid result = new Clothoid(x0, y0, direction0, 0, Af, length);
            return result;
        }

        /// <summary>
        /// Gets the curvature on the clothoid.
        /// </summary>
        /// <param name="t">t should be greater than 0 and smaller than 1.</param>
        /// <returns>Curvature at point t * length.</returns>
        public override double InterpolateCurvature(double t)
        {
            double s = t * _length;
            return CalculateCurvature(s);
        }

        /// <summary>
        /// Gets the direction on the clothoid.
        /// </summary>
        /// <param name="t">t should be greater than 0 and smaller than 1.</param>
        /// <returns>Direction at point t * length.</returns>
        public override double InterpolateDirection(double t)
        {
            double s = t * _length;
            return CalculateDirection(s);
        }

        /// <summary>
        /// Returns a point (x, y) on the curve, parameterized by t.
        /// Using simpson rule for integration.
        /// </summary>
        /// <param name="t">t should be greater than 0 and smaller than 1.</param>
        /// <returns></returns>
        public override Point2D InterpolatePoint2D(double t)
        {
            double s = t * _length;
            return CalculatePoint2D(s);
        }

        /// <summary>
        /// Returns a pose (x, y, theta) on the clothoid, parameterized by t.
        /// </summary>
        /// <param name="t">t should be greater than 0 and smaller than 1.</param>
        /// <returns>Pose at point t * length.</returns>
        public override Pose2D InterpolatePose2D(double t)
        {
            double s = t * _length;
            return CalculatePose2D(s);
        }

        /// <summary>
        /// Returns a posture (x, y, direction, curvature) on the clothoid, parameterized by t.
        /// </summary>
        /// <param name="t">t should be greater than 0 and smaller than 1.</param>
        /// <returns>Posture at point t * length.</returns>
        public override Posture2D InterpolatePosture2D(double t)
        {
            double s = t * _length;
            return CalculatePosture2D(s);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="s">Arclength s.</param>
        /// <returns></returns>
        public override Point2D CalculatePoint2D(double s)
        {
            double x = 0, y = 0;

            // length of this clothoid
            double L = s;
            // stepSize
            double h = L / ((double)NumIterations-1);

            double IntCos = 0;
            double IntSin = 0;

            double left = 0, mid = 0, right = 0;
            double pl = 0, pm = 0, pr = 0;

            if (_generatePointList)
            {
                _pointList.Add(new Point2D(_start_x + 0, _start_y + 0));
            }


            for (int i = 0; i < NumIterations-1; i++)
            {
                left = i * h;
                right = (i + 1) * h;
                mid = (left + right) / 2.0;

                pl = CalculateDirection(left);
                pm = CalculateDirection(mid);
                pr = CalculateDirection(right);

                IntCos += (h / 6.0) * (Math.Cos(pl) + 4 * Math.Cos(pm) + Math.Cos(pr));
                IntSin += (h / 6.0) * (Math.Sin(pl) + 4 * Math.Sin(pm) + Math.Sin(pr));

                if (_generatePointList)
                {
                    _pointList.Add(new Point2D(_start_x + IntCos, _start_y + IntSin));
                }
            }

            x = _start_x + IntCos;
            y = _start_y + IntSin;

            return new Point2D(x, y);
        }

        /// <summary>
        /// Calculates a number of points on the clothoid.
        /// </summary>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        public override List<Point2D> GetPoints(int numPoints)
        {
            _pointList = new List<Point2D>();

            _generatePointList = true;
            int oldNumIterations = NumIterations;
            NumIterations = numPoints;
            CalculatePoint2D(_length);
            NumIterations = oldNumIterations;
            _generatePointList = false;
            return _pointList;
        }

        /// <summary>
        /// Calculates the pose at arclength s of the clothoid.
        /// </summary>
        /// <param name="s">Arclength s.</param>
        /// <returns>Pose2D.</returns>
        public override Pose2D CalculatePose2D(double s)
        {
            Point2D p = CalculatePoint2D(s);
            double direction = CalculateDirection(s);

            return new Pose2D(p.X, p.Y, direction);
        }

        /// <summary>
        /// Calculates the posture at arclength s of the clothoid.
        /// </summary>
        /// <param name="s">Arclength s.</param>
        /// <returns>Posture2D.</returns>
        public override Posture2D CalculatePosture2D(double s)
        {
            Pose2D p = CalculatePose2D(s);
            double curvature = CalculateCurvature(s);
            return new Posture2D(p.X, p.Y, p.Direction, curvature);
        }

        /// <summary>
        /// Calculates the direction at arclength s of the clothoid.
        /// </summary>
        /// <param name="s">Arclength s.</param>
        /// <returns>Direction in radians.</returns>
        public override double CalculateDirection(double s)
        {
            double aa = _a * _a;
            if (_a < 0)
            {
                aa *= -1.0;
            }

            double direction = _start_direction;
            direction += (s * _start_curvature) + (s * s / (2 * aa));
            
            return direction;
        }

        /// <summary>
        /// Calculates the curvature at arclength s of the clothoid.
        /// </summary>
        /// <param name="s">Arclength s.</param>
        /// <returns>Curvature.</returns>
        public override double CalculateCurvature(double s)
        {
            double aa = _a * _a;
            if (_a < 0)
            {
                aa *= -1.0;
            }

            double curvature = _start_curvature;
            curvature += s / aa;
        
            return curvature;
        }
    }
}
