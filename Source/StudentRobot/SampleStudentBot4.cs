using Robocode;
using Robocode.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Serialization;

namespace CAP4053.Student
{
    public class SampleStudentBot4 : TeamRobot
    {
        int moveDirection = 1;//which way to move
        //private Random random = new Random();
        /**
         * run:  Tracker's main run function
         */
        public override void Run()
        {
            base.Run();
            while (true)
            {
                IsAdjustRadarForRobotTurn = true;//keep the radar still while we turn
                //IsAdjustRadarForGunTurn = true;
                IsAdjustGunForRobotTurn = true; // Keep the gun still when we turn
                base.Run();
                TurnRadarRightRadians(360);//keep turning radar right
            }
            
        }

        /**
         * onScannedRobot:  Here's the good stuff
         */
        public override void OnScannedRobot(ScannedRobotEvent e)
        {
            Console.Write("Robot Scanned");
            double absBearing = e.BearingRadians + HeadingRadians;//enemies absolute bearing
            double latVel = e.Velocity * Math.Sin(e.HeadingRadians - absBearing);//enemies later velocity
            //double gunTurnAmt;//amount to turn our gun
            SetTurnRadarLeftRadians(RadarTurnRemainingRadians);//lock on the radar
            /*if (random.NextDouble() > .9)
            {
                MaxVelocity = ((12 * random.NextDouble()) + 12);//randomly change speed
            }*/
            if (e.Distance > 150)
            {//if distance is greater than 150
                double gunTurnAmt = Utils.NormalRelativeAngle(absBearing - GunHeadingRadians + latVel / 22);//amount to turn our gun, lead just a little bit
                TurnGunRightRadians(gunTurnAmt); //turn our gun
                TurnRightRadians(Utils.NormalRelativeAngle(absBearing - HeadingRadians + latVel / Velocity));//drive towards the enemies predicted future location
                SetAhead((e.Distance - 140) * moveDirection);//move forward
                Fire(2);//fire
            }
            else
            {//if we are close enough...
                double gunTurnAmt = Utils.NormalRelativeAngle(absBearing - GunHeadingRadians + latVel / 15);//amount to turn our gun, lead just a little bit
                TurnGunRightRadians(gunTurnAmt);//turn our gun
                TurnLeft(-90 - e.Bearing); //turn perpendicular to the enemy
                Ahead((e.Distance - 140) * moveDirection);//move forward
                Fire(3);//fire
            }
        }
        public override void OnHitWall(HitWallEvent e)
        {
            moveDirection = -moveDirection;//reverse direction upon hitting a wall
        }

    }

}
