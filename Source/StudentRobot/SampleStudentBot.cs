using Robocode;
using Robocode.Util;
using System;
using System.Collections.Generic;

namespace CAP4053.Student
{
    public class SampleStudentBot : TeamRobot
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
            

            // build behavior tree
            BuildBehaviorTree();

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
                TurnRadarRight(360);

                // execute behavior tree
                BTRoot.Execute(this);

                // reset updated
                foreach (var enemy in enemyRobots)
                {
                    enemy.ResetUpdated();
                }
            }

        }
        //events

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
                currRobot.Update(evnt.Distance, evnt.Bearing, evnt.Heading, evnt.Velocity, evnt.Energy);
            }
            else
            {
                EnemyRobot newRobot = new EnemyRobot(evnt.Name, evnt.Distance, evnt.Bearing, evnt.Heading, evnt.Velocity, evnt.Energy);
                enemyRobots.Add(newRobot);
            }
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

        // build behavior tree
        private void BuildBehaviorTree()
        {
            // selector nodes
            SelectorNode rootUpdatedSelector = new SelectorNode("RootSelector", new List<BehaviorNode>());
            SelectorNode healthCheckSelector = new SelectorNode("RootSelector", new List<BehaviorNode>());
            SelectorNode lowHealthSelector = new SelectorNode("LowHealthSelector", new List<BehaviorNode>());
            SelectorNode highHealthSelector = new SelectorNode("HighHealthSelector", new List<BehaviorNode>());

            // Sequence nodes
            SequenceNode nullOrNotUpdatedSequence = new SequenceNode("NullOrNotUpdatedSequence");
            SequenceNode updatedSequence = new SequenceNode("UpdatedSequence");
            SequenceNode lowHealthSequence = new SequenceNode("LowHealthSequence");
            SequenceNode highHealthSequence = new SequenceNode("HighHealthSequence");


            SequenceNode enemyApproachingSequenceLow = new SequenceNode("EnemyApproachingSequence");
            SequenceNode hitByBulletSequenceLow = new SequenceNode("HitByBulletSequence");
            SequenceNode enemyFacingSequenceLow = new SequenceNode("EnemyFacingSequence");
            //SequenceNode enemyApproachingSequenceHigh = new SequenceNode("EnemyApproachingSequence");
            //SequenceNode hitByBulletSequenceHigh = new SequenceNode("HitByBulletSequence");
            //SequenceNode enemyFacingSequenceHigh = new SequenceNode("EnemyFacingSequence");
            SequenceNode bruteForceAttackSequence = new SequenceNode("BruteForceAttackSequence");

            // parallel nodes
            ParallelNode enemyApproachingParallel = new ParallelNode("EnemyApproachingParallel", 2, new List<BehaviorNode>());
            ParallelNode hitByBulletParallel = new ParallelNode("HitByBulletParallel", 2, new List<BehaviorNode>());
            ParallelNode enemyFacingParallel = new ParallelNode("EnemyFacingParallel", 2, new List<BehaviorNode>());

            // conditions
            ConditionNode isNull = new ConditionNode("IsNull", (robot) => !AnyEnemies(robot));
            ConditionNode isUpdated = new ConditionNode("IsUpdated", (robot) => AnyEnemies(robot));
            ConditionNode isHealthLow = new ConditionNode("IsHealthLow", (robot) => IsHealthLow(robot, GetBiggestThreat()));
            ConditionNode isHealthNotLow = new ConditionNode("IsHealthNotLow", (robot) => !IsHealthLow(robot, GetBiggestThreat()));
            ConditionNode isHealthVeryHigh = new ConditionNode("isHealthVeryHigh", (robot) => IsHealthVeryHigh(robot, GetBiggestThreat()));
            ConditionNode isEnemyApproaching = new ConditionNode("EnemyAppraoching", (robot) => EnemyApproaching(robot, GetBiggestThreat()));
            ConditionNode isHitByBullet = new ConditionNode("HitByBullet", (robot) => HitByBullet(robot));
            ConditionNode isEnemyFacing = new ConditionNode("EnemyFacing", (robot) => EnemyApproaching(robot, GetBiggestThreat()));

            // actions
            ActionNode chargeEnemy = new ActionNode("ChargeEnemy", (robot) => ChargeEnemy(robot, GetBiggestThreat()));
            ActionNode fireAtEnemy = new ActionNode("FireAtEnemy", (robot) => FireAtEnemy(robot, GetBiggestThreat()));
            ActionNode retreatBullet = new ActionNode("RetreatBullet", (robot) => Retreat(robot, GetBiggestThreat(), "bullet"));
            ActionNode retreatEnemy = new ActionNode("retreatEnemy", (robot) => Retreat(robot, GetBiggestThreat(), "enemy"));
            ActionNode randomMoves = new ActionNode("GenerateRandomMovement", (robot) => GenerateRandomMovement(robot));
            


            // if null or not updated
            nullOrNotUpdatedSequence.AddChild(isNull);
            nullOrNotUpdatedSequence.AddChild(randomMoves);
            rootUpdatedSelector.AddChild(nullOrNotUpdatedSequence);

            // if updated
            updatedSequence.AddChild(isUpdated);
            rootUpdatedSelector.AddChild(updatedSequence);
            updatedSequence.AddChild(healthCheckSelector);


            // if low health
            lowHealthSequence.AddChild(isHealthLow);
            lowHealthSequence.AddChild(lowHealthSelector);
            healthCheckSelector.AddChild(lowHealthSequence);

            // if enemy approaching
            enemyApproachingSequenceLow.AddChild(isEnemyApproaching);
            enemyApproachingSequenceLow.AddChild(fireAtEnemy);
            enemyApproachingSequenceLow.AddChild(retreatEnemy);
            //enemyApproachingSequenceLow.AddChild(enemyApproachingParallel);
            lowHealthSelector.AddChild(enemyApproachingSequenceLow);

            // if hit by bullet
            hitByBulletSequenceLow.AddChild(isHitByBullet);
            hitByBulletSequenceLow.AddChild(fireAtEnemy);
            hitByBulletSequenceLow.AddChild(retreatBullet);
            //hitByBulletSequenceLow.AddChild(hitByBulletParallel);
            hitByBulletSequenceLow.AddChild(randomMoves);
            lowHealthSelector.AddChild(hitByBulletSequenceLow);

            // if enemy facing
            enemyFacingSequenceLow.AddChild(isEnemyFacing);
            enemyFacingSequenceLow.AddChild(fireAtEnemy);
            enemyFacingSequenceLow.AddChild(retreatEnemy);
           // enemyFacingSequenceLow.AddChild(enemyFacingParallel);
            enemyFacingSequenceLow.AddChild(randomMoves);
            lowHealthSelector.AddChild(enemyFacingSequenceLow);

            // else
            lowHealthSelector.AddChild(fireAtEnemy);
            lowHealthSelector.AddChild(retreatEnemy);
            lowHealthSelector.AddChild(randomMoves);

            // if not low health
            highHealthSequence.AddChild(isHealthNotLow);
            highHealthSequence.AddChild(highHealthSelector);
            healthCheckSelector.AddChild(highHealthSequence);

            // if very high health charge
            bruteForceAttackSequence.AddChild(isHealthVeryHigh);
            bruteForceAttackSequence.AddChild(chargeEnemy);
            bruteForceAttackSequence.AddChild(fireAtEnemy);
            bruteForceAttackSequence.AddChild(retreatEnemy);
            highHealthSelector.AddChild(bruteForceAttackSequence);

            // if enemy approaching
            highHealthSelector.AddChild(enemyApproachingSequenceLow);

            // if hit by bullet
            highHealthSelector.AddChild(hitByBulletSequenceLow);

            // if enemy facing
            highHealthSelector.AddChild(enemyFacingSequenceLow);

            // else
            highHealthSelector.AddChild(fireAtEnemy);
            highHealthSelector.AddChild(retreatEnemy);
            highHealthSelector.AddChild(randomMoves);

            BTRoot = rootUpdatedSelector;

        }

        // conditions/actions
        
        // enemy null or not updated
        private bool AnyEnemies(TeamRobot robot)
        {
            return (GetBiggestThreat() != null);
        }

        // bool Enemy Approaching (charging to attack)
        private bool EnemyApproaching(TeamRobot robot, EnemyRobot enemy)
        {
            // velocity > 0; distance close; Heading pointed towards my position
            if (enemy.Distance < 400 && EnemyFacing(robot, enemy))
            {
                return true;
            }
            return false;
        }

        // enemy facing - possible long range shot -- change to facing toward position
        private bool EnemyFacing(TeamRobot robot, EnemyRobot enemy)
        {
            // Heading pointed towards my position
            double bearingToEnemy = enemy.Bearing;
            //double angleDiff = Math.Abs(Utils.NormalRelativeAngleDegrees(enemy.Heading - bearingToEnemy));
            double angleDiff = Math.Abs(bearingToEnemy);
            double threshold = 30;
            return angleDiff < threshold;

        }

        // health compared to enemy passed in
        private bool IsHealthLow(TeamRobot robot, EnemyRobot enemy)
        {
            double healthProp = robot.Energy/enemy.Energy;
            double threshold = 0.3;
            return healthProp < threshold;

        }

        // health compared to enemy is higher
        private bool IsHealthVeryHigh(TeamRobot robot, EnemyRobot enemy)
        {
            double healthProp = robot.Energy / enemy.Energy;
            return healthProp > 0.8;
        }

        // fire at enemy
        private void FireAtEnemy(TeamRobot robot, EnemyRobot enemy)
        {
            // align gun with robot heading
            double angleToAlign = Utils.NormalRelativeAngleDegrees(robot.Heading - robot.GunHeading);
            robot.TurnGunRight(angleToAlign);

            // align gun to enemy bearing
            double angleToEnemy = Utils.NormalRelativeAngleDegrees(enemy.Bearing);
            robot.TurnGunRight(angleToEnemy);

            if (enemy.Distance > 500 || IsHealthLow(robot, enemy))
            {
                robot.Fire(1);
            }
            else if (enemy.Distance >200 && enemy.Distance < 500)
            {
                robot.Fire(2);
            }
            else if (enemy.Distance < 200)
            {
                robot.Fire(3);
            }
        }

        // retreat from danger (enemy or bullet)
        private void Retreat(TeamRobot robot, EnemyRobot enemy, String danger)
        {
            if (danger == "enemy")
            {
                double angleToEnemy = Utils.NormalRelativeAngleDegrees(enemy.Bearing);
                robot.TurnRight(angleToEnemy + 100);
                robot.Back(400);
            }
            else if (danger == "bullet")
            {
                robot.TurnRight(90 - bulletBearing);
                robot.Back(100);
            }
        }

        // hit by bullet
        private bool HitByBullet(TeamRobot robot)
        {
            return bulletHit;
        }

        // my bullet hit 
        private bool EnemyHit(TeamRobot robot)
        {
            return enemyHit;
        }

        // random movement
        private void GenerateRandomMovement(TeamRobot robot)
        {
            /*double turnAngle = Utils.NormalRelativeAngleDegrees(Utils.GetRandom().NextDouble() * 180 - 90);
            double turnAngle2 = Utils.NormalRelativeAngleDegrees(Utils.GetRandom().NextDouble() * 180 - 90);
            double turnAngle3 = Utils.NormalRelativeAngleDegrees(Utils.GetRandom().NextDouble() * 180 - 90);
            double moveDistanceAhead = Utils.GetRandom().NextDouble() * 400;
            double moveDistanceBack = Utils.GetRandom().NextDouble() * 400;
            double moveDistanceAhead2 = Utils.GetRandom().NextDouble() * 400;
            */


            double turnAngle = 40;
            double moveDistance = 200;

            /*robot.TurnRight(turnAngle);
            robot.Ahead(moveDistanceAhead);
            robot.TurnRight(turnAngle2);
            robot.Back(moveDistanceBack);
            robot.TurnRight(turnAngle3);
            robot.Ahead(moveDistanceAhead2);
            */
        }

        // charge enemy
        private void ChargeEnemy(TeamRobot robot, EnemyRobot enemy)
        {
            robot.TurnRight(enemy.Bearing);
            robot.Ahead(enemy.Distance - 100);
        }



    }

    // behavior tree node interface
    public interface BehaviorNode
    {
        NodeState Execute(TeamRobot robot);
    }

    // define node states
    public enum NodeState
    {
        Success,
        Failure,
        Running
    }

    // sequence node (child nodes in sequence until one fails)
    public class SequenceNode : BehaviorNode
    {
        private readonly string name;
        private readonly List<BehaviorNode> childNodes = new List<BehaviorNode>();

        public SequenceNode(string name)
        {
            this.name = name;
        }

        public void AddChild(BehaviorNode child)
        {
            childNodes.Add(child);
        }

        public NodeState Execute(TeamRobot robot)
        {
            Console.WriteLine($"Executing sequence node: {name}");
            foreach (var child in childNodes)
            {
                var childState = child.Execute(robot);
                if (childState != NodeState.Success)
                {
                    Console.WriteLine($"Executing child node: {child.GetType().Name} and State: {childState}");
                    return childState;
                }
            }
            return NodeState.Success;
        }


    }

    // selector node (child nodes until one succeeds)
    public class SelectorNode : BehaviorNode
    {
        private readonly string name;
        private readonly List<BehaviorNode> childNodes = new List<BehaviorNode>();

        public SelectorNode(string name, List<BehaviorNode> childNodes)
        {
            this.name = name;
            //this.childNodes = childNodes;
        }
        public void AddChild(BehaviorNode child)    
        {  
            childNodes.Add(child); 
        }
        public NodeState Execute(TeamRobot robot)
        {
            Console.WriteLine($"Executing selector node: {name}");
            foreach (var child in childNodes)
            {
                var childState = child.Execute(robot);
                if (childState == NodeState.Success)
                {
                    Console.WriteLine();
                    return NodeState.Success;
                }
            }
            return NodeState.Failure;
        }
    }

    // parallel node (executes child nodes in parallel)
    public class ParallelNode : BehaviorNode
    {
        private readonly string name;
        private readonly List<BehaviorNode> childNodes = new List<BehaviorNode>();
        private readonly int requiredSuccesses;

        public ParallelNode(string name, int requiredSuccesses, List<BehaviorNode> childNodes)
        {
            this.name = name;
            this.requiredSuccesses = requiredSuccesses;
            this.childNodes = childNodes;
        }
        public void AddChild(BehaviorNode child)
        {
            childNodes.Add(child);
        }
        public NodeState Execute(TeamRobot robot)
        {
            Console.WriteLine($"Executing parallel node: {name}");
            int successes = 0;

            foreach (var child in childNodes)
            {
                var childState = child.Execute(robot);
                if (childState == NodeState.Success)
                {
                    successes++;
                    if (successes >= requiredSuccesses)
                    {
                        Console.WriteLine();
                        return NodeState.Success;
                    }
                }
            }
            return NodeState.Failure;

        }
    }

    // condition node (checks condition and returns success or failure)
    public class ConditionNode : BehaviorNode
    {
        private readonly string name;
        private readonly Func<TeamRobot, bool> condition;

        public ConditionNode(string name, Func<TeamRobot, bool> condition)
        {
            this.name = name;
            this.condition = condition;
        }

        public NodeState Execute(TeamRobot robot)
        {
            if (condition(robot))
            {
                Console.WriteLine($"Condition '{name}' succeeded.");
                return NodeState.Success;
            }
            else
            {
                Console.WriteLine($"Condition '{name}' failed.");
                return NodeState.Failure;
            }
        }
    }

    // action node (performs an action)
    public class ActionNode : BehaviorNode
    {
        private readonly string name;
        private readonly Action<TeamRobot> action;

        public ActionNode(string name, Action<TeamRobot> action)
        {
            this.name = name;
            this.action = action;
        }

        public NodeState Execute(TeamRobot robot)
        {
            action(robot);
            Console.WriteLine($"Action '{name})' executed.");
            return NodeState.Success;
        }
    }
}
