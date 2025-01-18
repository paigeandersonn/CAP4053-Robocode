using Robocode;
using Robocode.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace CAP4053.Student
{
    public class UrABot : TeamRobot
    {
        // variables
        private List<EnemyRobot> enemyRobots = new List<EnemyRobot>();
        private double myX;
        private double myY;
        private bool bulletHit;
        private double bulletBearing;
        private double moveDirection;
        EnemyRobot enemyRobot;

        // begin run function
        public override void Run()
        {
            // initialize variables and complete 2 radar spins
            this.BodyColor = Color.MediumPurple;
            this.RadarColor = Color.MediumPurple;
            this.GunColor = Color.MediumPurple;
            this.BulletColor = Color.MediumPurple;
            bulletBearing = -1;
            bulletHit = false;
            enemyRobot = new EnemyRobot();
            IsAdjustGunForRobotTurn = true;
            IsAdjustRadarForRobotTurn = true;
            IsAdjustRadarForGunTurn = true;
            moveDirection = 1;
            TurnRadarRight(360);
            TurnRadarRight(360);

            // Track enemy, make decision & execute
            while (true)
            {
                bulletHit = false;
                //enemyRobot.ResetUpdated();
                NarrowBeamTracking(enemyRobot);
                MakeDecision();
                Execute();
            }
        }

        // On scanned robot, set previous energy, update if not teammate
        public override void OnScannedRobot(ScannedRobotEvent evnt)
        {
            enemyRobot.PrevEnergy = enemyRobot.Energy;
            if (!IsTeammate(evnt.Name))
            {
                enemyRobot.Update(evnt);
            }
            
        }

        // hit wall maneuvers
        public override void OnHitWall(HitWallEvent evnt)
        {
            base.OnHitWall(evnt);
            //Ahead(100);

            // if enemy nearby: retreat, otherwise move amount & generate random movement
        }

        // hit by bullet events
        public override void OnHitByBullet(HitByBulletEvent evnt)
        {
            //base.OnHitByBullet(evnt);
            bulletHit = true;
            bulletBearing = evnt.Bearing;
            //Retreat("bullet");
        }


        // narrow beam tracking https://www.cse.chalmers.se/~bergert/robowiki-mirror/RoboWiki/robowiki.net/wiki/Radar.html
        private void NarrowBeamTracking(EnemyRobot enemy)
        {
            Console.WriteLine("Narrow Beam Called");
            double absBearing = Heading + enemy.Bearing;
            double radarTurn = absBearing - RadarHeading;
            SetTurnRadarRight(2.0 * Utils.NormalRelativeAngleDegrees(radarTurn));

            //double gunTurn = Utils.NormalRelativeAngleDegrees(enemy.Bearing - GunHeading);
            //SetTurnGunRight(gunTurn);   
        }

        
        // decision tree conditions: health, enemy distance, near walls, hit by bullet
        
        // returns health proportion value
        public double HealthProp()
        {
            return (Energy / enemyRobot.Energy);
        }

        // returns enemy proximity
        public double EnemyProximity(EnemyRobot enemy)
        {
            return enemy.Distance;
        }
        
        // returns bool if enemy is approaching 
        public bool EnemyApproaching()
        {
            // distance close; Heading pointed towards my position
            if (enemyRobot.Distance < 600 && EnemyFacing())
            {
                return true;
            }
            return false;
        }

        // returns bool if enemy facing - possible long range shot -- change to facing toward position
        public bool EnemyFacing()
        {
            // Heading pointed towards my position
            double bearingToEnemy = enemyRobot.Bearing;
            //double angleDiff = Math.Abs(Utils.NormalRelativeAngleDegrees(enemy.Heading - bearingToEnemy));
            double angleDiff = Math.Abs(bearingToEnemy);
            double threshold = 30;
            return angleDiff < threshold;

        }

        // returns bool if change in enemy energy detected (enemy fired)- https://web.archive.org/web/20170217094018/http://www.ibm.com/developerworks/java/library/j-dodge/
        public bool EnemyFired()
        {
            double energyChange = enemyRobot.PrevEnergy + enemyRobot.Energy;    
            if (energyChange > 0 && energyChange <= 3)
            {
                return true;
            }
            return false;
        }
        
        // returns bool if X or Y position is with 18 of wall
        public bool CloseToWall()
        {
            myX = this.X;
            myY = this.Y;
            if (myX - 18.0 <= 0 || myX + 18.0 >= BattleFieldWidth || myY - 18.0 <= 0 || myY + 18.0 >= BattleFieldHeight)
            {
                return true;
            }
            return false;
        }


        // decision tree strategies: actions: fire(pass in distance), charge(enemy), avoid walls(turn away), evade, defend

        // squaring off https://mark.random-article.com/weber/java/robocode/lesson5.html
        public void SquareOff()
        {
            SetTurnRight(enemyRobot.Bearing + 90);
            if (Time % 10 == 0)
            {
                moveDirection *= -1;
                SetAhead(500 * moveDirection);
            }
            if (Velocity == 0)
            {
                moveDirection *= -1;
            }

            SetAhead(1000 * moveDirection);
            FireAtEnemy();
        }
        // avoiding fire DodgeBot: https://web.archive.org/web/20170217094018/http://www.ibm.com/developerworks/java/library/j-dodge/
        public void AvoidFire()
        {
            moveDirection *= -1;
            SetAhead((enemyRobot.Distance / 4 + 25) * moveDirection);
        }

        // fire bullet at enemy- utilize linear targeting https://robowiki.net/wiki/Linear_Targeting 
        private void FireAtEnemy()
        {
            //double angleToAlign = Utils.NormalRelativeAngleDegrees(Heading - GunHeading + enemy.Bearing);
            double absBearing = Heading + enemyRobot.Bearing;
            SetTurnGunRight(Utils.NormalRelativeAngleDegrees(absBearing - GunHeading + (6*enemyRobot.Velocity * Math.Sin(enemyRobot.Heading - absBearing) / 13.0)));
            //SetTurnGunRight(angleToAlign);

            if (enemyRobot.Distance > 300)
            {
                SetFire(1);
            }
            else if (enemyRobot.Distance > 150 && enemyRobot.Distance < 300)
            {
                SetFire(2);
            }
            else if (enemyRobot.Distance < 150)
            {
                SetFire(3);
            }
            //SetFire(Math.Min(400 / enemy.Distance, 3));
        }

        public void ChargeEnemy()
        {
            SetTurnRight(Utils.NormalRelativeAngleDegrees(enemyRobot.Bearing));
            SetAhead(enemyRobot.Distance);
            FireAtEnemy();
        }

        public void AvoidWall()
        {
            moveDirection *= -1;
            SetAhead(300 * moveDirection);
            /*if (myX - 18.0 <= 0)
            {
                
            }
            else if (myX + 18.0 >= BattleFieldWidth){

            }
            else if ( myY - 18.0 <= 0)
            {

            }
            else if (myY + 18.0 >= BattleFieldHeight) 
            { 

            }*/
        }

        // retreat from danger (enemy or bullet) - spiraling out  https://mark.random-article.com/weber/java/robocode/lesson5.html
        public void Retreat(String danger)
        {
            if (danger == "enemy")
            {
                //SetTurnRight(Utils.NormalRelativeAngleDegrees(enemyRobot.Bearing + 90));
                SetTurnRight(Utils.NormalRelativeAngleDegrees(enemyRobot.Bearing + 90 + (15 * moveDirection)));
            }
            else if (danger == "bullet")
            {
                //SetTurnRight(Utils.NormalRelativeAngleDegrees(bulletBearing + 90));
                SetTurnRight(Utils.NormalRelativeAngleDegrees(bulletBearing + 90 + (15 * moveDirection)));
               
            }
            if (Time % 10 == 0)
            {
                moveDirection *= -1;
                SetBack(500 * moveDirection);
            }
            if (Velocity == 0)
            {
                moveDirection *= -1;
            }

            SetBack(1000 * moveDirection);
            FireAtEnemy();
        }
        private void MakeDecision()
        {
            DecisionNode root = BuildDecisionTree();
            EvaluateNode(root);
        }
        private DecisionNode BuildDecisionTree()
        {
            DecisionNode root = new DecisionNode
            {
                Condition = () => EnemyApproaching(),
                //Condition = () => CloseToWall(),
                //Action = () => AvoidWall(),
                //Action = () => ChargeOrRetreat(),
                Left = new DecisionNode
                {

                    Condition = () => true,
                    //Condition = () => HealthProp() < 1.6,
                    //ction = () => Retreat("enemy")

                    //Action = () => Retreat("enemy"),
                    Left = new DecisionNode
                    {
                        Condition = () => HealthProp() < 1.6,
                        Action = () => Retreat("enemy")
                    },
                    Right = new DecisionNode
                    {
                        Condition = () => HealthProp() >= 1.6,
                        Action = () => SquareOff() 
                        //Action = () => ChargeEnemy()
                    }
                },
                Right = new DecisionNode
                {
                    //Condition = () => (HealthProp() >= .5),
                    Condition = () => EnemyFired(),
                    // Condition = () => CloseToWall(),
                    //Action = () => DoMove(),
                    Left = new DecisionNode
                    {
                        //Condition = () => bulletHit,
                        Condition = () => true,
                        Action = () => AvoidFire()
                        //Action = () => Retreat("enemy")
                   
                        //Action = () => AvoidWall(),
                    },
                    Right = new DecisionNode
                    {
                        Condition = () => true,
                        Action = () => SquareOff()
                    }

                }
            };
            return root;
        }
        private void EvaluateNode(DecisionNode node)
        {
            if (node == null)
            {
                return;
            }
            if (node.Condition())
            {
                if (node.Action != null)
                {
                    node.Action();
                }
                else
                {
                    EvaluateNode(node.Left);
                }               
                
            }
            else
            {
                EvaluateNode(node.Right);
            }
        }

        // enemy Robot class
        public class EnemyRobot
        {
            public string Name { get; }
            public double Distance { get; private set; }
            public double Bearing { get; private set; }
            public double Heading { get; private set; }
            public double Velocity { get; private set; }
            public double Energy { get; private set; }
            public bool Updated { get; private set; }
            public double PrevEnergy { get; set; }

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
            public EnemyRobot()
            {
                Name = "";
                Distance = -1;
                Bearing = -1;
                Heading = -1;
                Velocity = -1;
                Energy = -1;
                PrevEnergy = -1;
                Updated = true;
            }

            public void Update(ScannedRobotEvent evnt)
            {
                Distance = evnt.Distance;
                Bearing = evnt.Bearing;
                Heading = evnt.Heading;
                Velocity = evnt.Velocity;
                Energy = evnt.Energy;
                Updated = true;
            }

            public void ResetUpdated()
            {
                Updated = false;
            }

        }

    }
    public class DecisionNode
    {
        public Func<bool> Condition { get; set; }
        public Action Action { get; set; }
        public DecisionNode Left { get; set; }
        public DecisionNode Right { get; set; }

    }
}
