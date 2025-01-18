using Robocode;
using Robocode.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Xml.Serialization;

namespace CAP4053.Student
{
    public class SampleStudentBot2 : TeamRobot
    {
        // variables
        private List<EnemyRobot> enemyRobots = new List<EnemyRobot>();
        private double myEnergy;
        private double myX;
        private double myY;
        private double myVelocity;
        private double myHeading;
        private double myGunHeading;
        private double myRadarHeading;
        private bool bulletHit;
        private double bulletBearing;
        private bool enemyHit;

        private BehaviorNode BTRoot;

        public override void Run()
        {
            // set radar and gun to move individually
            

            base.Run();
            while (true)
            {
                IsAdjustGunForRobotTurn = true;
                IsAdjustRadarForRobotTurn = true;
                IsAdjustRadarForGunTurn = true;

                // collect individual values
                bulletHit = false;
                enemyHit = false;
                bulletBearing = 0;
                myEnergy = Energy;
                myX = X;    
                myY = Y;    
                myVelocity = Velocity;
                myHeading = Heading;
                myGunHeading = GunHeading;
                myRadarHeading = RadarHeading;

                // collect enemy data
                TurnRadarRight(45);
                Execute();

                // reset updated
                foreach (var enemy in enemyRobots)
                {
                    enemy.ResetUpdated();
                }
                Execute();
            }
        }       

        // circular targeting - robocode wiki
        private void Targeting(TeamRobot robot, EnemyRobot enemy)
        {
            double robotX = robot.X;
            double robotY = robot.Y;
            double myHeading = robot.HeadingRadians;
            double targetX = robotX + enemy.Distance * Math.Sin(enemy.Bearing + myHeading);
            double targetY = robotY + enemy.Distance * Math.Cos(enemy.Bearing + myHeading);
            double enemyHeading = enemy.Heading;
            double headingChange = enemyHeading - enemy.prevHeading;
            double deltaTime = 0;
            double battleFieldWidth = this.BattleFieldWidth;
            double battleFieldHeight = this.BattleFieldHeight;
            double predX = targetX;
            double predY = targetY;
            double dist = Math.Sqrt(Math.Pow(predX - robotX, 2) + Math.Pow(predY - robotY, 2));
            double errorMargin = 10;
            double angleToAlign = Utils.NormalRelativeAngleDegrees(robot.Heading - robot.GunHeading);
            robot.TurnGunRight(angleToAlign);

            while ((deltaTime*(calcMaxDistance(calcBulletPower(dist), enemy)) + errorMargin) < dist)
            {
                predX += Math.Sin(enemy.Heading) * enemy.Velocity;
                predY += Math.Cos(enemy.Heading) * enemy.Velocity;
                enemyHeading += headingChange;

                if (predX < 18.0 || predY < 18.0 || predX >battleFieldWidth - 18.0 || predY > battleFieldHeight - 18.0)
                {
                    predX = Math.Min(Math.Max(18.0, predX), battleFieldWidth - 18.0);
                    predY = Math.Min(Math.Max(18.0, predY), battleFieldHeight - 18.0);
                    Execute();
                    break;

                }

                dist = Math.Sqrt(Math.Pow(predX - robot.X, 2) + Math.Pow(predY - robot.Y, 2));
                deltaTime++;
            }
            double theta = Utils.NormalAbsoluteAngle(Math.Atan2(predX - robotX, predY - robotY));
            double radarTurn = Utils.NormalRelativeAngle(theta - robot.RadarHeading);
            double gunTurn = Utils.NormalRelativeAngle(theta - robot.GunHeading);
            robot.SetTurnRadarRightRadians(radarTurn);
            robot.SetTurnGunRightRadians(gunTurn);

            double bulletPower = calcBulletPower(dist);
            robot.Fire(bulletPower);

        }
        // targeting helper functions
        private double calcMaxDistance(double bulletPower, EnemyRobot enemy)
        {
            // bullet speed
            double bulletSpeed = 20 - 3*bulletPower;

            // max travel distance of enemy during bullet flight
            double maxDist = bulletSpeed * enemy.Velocity;

            return maxDist;

        }

        private double calcBulletPower(double distance)
        {
            if (distance < 200)
            {
                return 3.0;
            }
            else if (distance >= 200 || distance < 500)
            {
                return 2.0;
            }
            else
            {
                return 1.0;
            }

        }

        // update onhitbybullet 
        public override void OnHitByBullet(HitByBulletEvent evnt)
        {
            bulletHit = true;
            bulletBearing = evnt.Bearing;
        }

        // update on bullet hit
        public override void OnBulletHit(BulletHitEvent evnt)
        {
            //base.OnBulletHit(evnt);
            enemyHit = true;
        }

        // update on hit wall
        public override void OnHitWall(HitWallEvent evnt)
        {
            base.OnHitWall(evnt);

            // if enemy nearby: retreat, otherwise move amount & generate random movement
        }


        // OnScannedRobot method to collect data on enemy robots nearby
        public override void OnScannedRobot(ScannedRobotEvent evnt)
        {
            EnemyRobot currRobot = enemyRobots.Find(robot => robot.Name == evnt.Name);
            Console.WriteLine("robot scanned");

            if (currRobot != null)
            {
                currRobot.prevHeading = currRobot.Heading;
                currRobot.Update(evnt.Distance, evnt.BearingRadians, evnt.HeadingRadians, evnt.Velocity, evnt.Energy);
                
            }

            else
            {
                EnemyRobot newRobot = new EnemyRobot(evnt.Name, evnt.Distance, evnt.BearingRadians, evnt.HeadingRadians, evnt.Velocity, evnt.Energy);
                enemyRobots.Add(newRobot);
            }
            Targeting(this, GetBiggestThreat());
            //FireAtEnemy(this, GetBiggestThreat());
        }


        // class to represent an enemy robot
        public class EnemyRobot
        {
            public string Name { get; }
            public double Distance { get; private set; }
            public double Bearing { get; private set; }
            public double Heading { get; private set; }
            public double Velocity { get; private set; }
            public double Energy { get; private set; }
            public bool Updated { get; private set; }
            public double prevHeading = 0;

            // robot name, distance to my robot, Bearing (angle from my heading to robot), enemy heading (angle facing), current velocity
            public EnemyRobot(string name, double distance, double bearing, double heading, double velocity, double energy)
            {
                Name = name;
                Distance = distance;
                Bearing = bearing;
                Heading = heading;
                Velocity = velocity;
                Energy = energy;
                Updated = true;
            }

            public void Update(double distance, double bearing, double heading, double velocity, double energy)
            {
                Distance = distance;
                Bearing = bearing;
                Heading = heading;
                Velocity = velocity;
                Energy = energy;
                Updated = true;
            }

            public void ResetUpdated()
            {
                Updated = false;
            }
        }

        // get closest enemy threat
        private EnemyRobot GetBiggestThreat()
        {
            int dist = int.MaxValue;
            EnemyRobot biggestThreat = null;
            foreach(EnemyRobot enemy in enemyRobots)
            {
                if (enemy.Distance < dist)
                {
                    biggestThreat = enemy;
                }
                Console.WriteLine($"Biggest Threat Identified: '{enemy}'");
            }
            return biggestThreat;
        }

    }

}
