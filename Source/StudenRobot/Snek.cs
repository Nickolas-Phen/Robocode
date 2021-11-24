using Robocode;
using Robocode.Util;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace CAP4053.Student
{
    public class Snek : TeamRobot
    {
        
        bool melee = true;
        FSM mach = new FSM();
        public override void Run()
        {
            IsAdjustRadarForGunTurn = true;
            IsAdjustGunForRobotTurn = true;
            
            SetAllColors(Color.ForestGreen);
            while (true)
            {
                if (mach.curState == FSM.State.idle)
                {
                    SetTurnRadarLeft(Double.PositiveInfinity);
                    mach.nextState(FSM.Command.scan);
                }
                Execute();
            }

        }
        public override void OnScannedRobot(ScannedRobotEvent e)
        {
            //Out.WriteLine("after: " + mach.curState);
            if (!IsTeammate(e.Name)/*!e.Name.Contains("SampleStudentBot")*/)
            {
                if (mach.curState == FSM.State.scanning)
                {
                    trace(e);
                    mach.nextState(FSM.Command.trace);
                }
                if (mach.curState == FSM.State.tracing)
                {
                    target(e);
                    mach.nextState(FSM.Command.target);
                }
                if (mach.curState == FSM.State.targeting || mach.curState==FSM.State.strafing)
                {
                    if (nearWall())
                    {
                        wallAvoid(e);
                        mach.nextState(FSM.Command.wallAvoid);
                    }
                    else
                    {
                        strafe(e);
                        mach.nextState(FSM.Command.strafe);
                    }
                    if(mach.curState == FSM.State.avoiding || mach.curState == FSM.State.strafing)
                    {
                        mach.nextState(FSM.Command.scan);
                    }
                }
                //Out.WriteLine("after: "+mach.curState);
                
                //fire(e);
            }

        }

        public void trace(ScannedRobotEvent e)//basically some form of infinity lock
        {
            SetTurnRadarLeft(RadarTurnRemaining);
        }

        public void target(ScannedRobotEvent e)
        {
            double power = Math.Min(3.0,Energy);
            double dist = e.Distance;
            if (dist < 50)
            {
                power = 1 / (dist / 100) * 2;
            }
            else if (dist > 400)
            {
                power = 1;
            }
            double absBearing = HeadingRadians + e.BearingRadians;
            SetTurnGunRightRadians(Utils.NormalRelativeAngle(absBearing -GunHeadingRadians + Math.Asin((e.Velocity * Math.Sin(e.HeadingRadians -absBearing)) / (20-3*power))));
            SetFire(power);
        }

        public void strafe(ScannedRobotEvent e)//something like antigrav
        {

            double dist = e.Distance;
            double repelX = 1 / ((dist * Math.Sin(HeadingRadians + e.BearingRadians)) / BattleFieldWidth);
            double repelY = 1 / ((dist * Math.Cos(HeadingRadians + e.BearingRadians)) / BattleFieldHeight);
            //Out.WriteLine("repelX: " + repelX + " repelY: " + repelY);

            if (dist < 400)
            {
                //Out.WriteLine("Ang: " + Math.Atan(repelX / repelY) + " pow: " + (Math.Sqrt(Math.Pow(repelX, 2) + Math.Pow(repelY, 2))+100));
                SetTurnLeft(HeadingRadians + e.Bearing + Math.Atan(repelX / repelY));
                if (melee)//Changing the Set method changes i.e SetBack=melee bot while SetAhead=strafe
                    SetBack(Math.Sqrt(Math.Pow(repelX, 2) + Math.Pow(repelY, 2)) + 100);
                else
                    SetAhead(Math.Sqrt(Math.Pow(repelX, 2) + Math.Pow(repelY, 2)) + 100);
            }
            else
            {
                SetTurnRight(Utils.NormalRelativeAngleDegrees(e.Heading + e.Bearing));

                SetAhead(100);

            }


        }

        public void wallAvoid(ScannedRobotEvent e)
        {
            double closeX = (Math.Abs(X - (BattleFieldWidth / 2)) / (BattleFieldWidth / 2));
            double closeY = (Math.Abs(Y - (BattleFieldHeight / 2)) / (BattleFieldHeight / 2));
            int dir = 0;

            double c = Math.Sqrt(Math.Pow(closeX, 2) + Math.Pow(closeY, 2)) * 100 + 100;
            //Out.WriteLine("X: " + closeX + " Y: " + closeY+" C: "+c+" nearWall: "+nearWall());

            SetTurnRight(HeadingRadians + e.BearingRadians + (Math.PI - Math.Atan(closeX / closeY)));// 
            //WaitFor(new TurnCompleteCondition(this));
            //Out.WriteLine(Math.PI / 2 - Math.Tan(closeX / closeY));//fixe tan function NaN returned for bad quadrants
            SetAhead(c);


        }

        public bool nearWall()
        {
            double xPos = X;
            double yPos = Y;
            if ((xPos < 200 || xPos > BattleFieldWidth - 200) || (yPos < 200 || yPos > BattleFieldHeight - 200))
                return true;
            return false;
        }

        public class FSM
        {
            public enum State
            {
                /*running,
                ended,
                paused,*/
                idle,
                scanning,
                tracing,
                targeting,
                strafing,
                avoiding
            }
            public enum Command
            {
                scan,
                trace,
                target,
                strafe,
                wallAvoid
            }
            class Transition
            {
                State curState;
                Command command;

                public Transition(State s, Command c)
                {
                    curState = s;
                    command = c;
                }
                public override int GetHashCode()
                {
                    return 71 + 93 * curState.GetHashCode() + 69 * command.GetHashCode();
                }
                public override bool Equals(object obj)
                {
                    Transition other = obj as Transition;
                    if (other != null)
                        return this.curState == other.curState && this.command == other.command;
                    return false;
                }
            }
            Dictionary<Transition, State> transitions;
            public State curState { get; private set; }

            public FSM()
            {
                curState = State.idle;
                transitions = new Dictionary<Transition, State>
            {
                { new Transition(State.idle,Command.scan),State.scanning},
                { new Transition(State.scanning,Command.trace),State.tracing},
                { new Transition(State.scanning,Command.scan),State.scanning},
                { new Transition(State.tracing,Command.target),State.targeting},
                { new Transition(State.targeting,Command.strafe),State.strafing},
                { new Transition(State.targeting,Command.wallAvoid),State.avoiding},
                { new Transition(State.strafing,Command.wallAvoid),State.avoiding},
                { new Transition(State.strafing,Command.scan),State.scanning},
                { new Transition(State.strafing,Command.strafe),State.strafing},
                { new Transition(State.avoiding,Command.scan),State.scanning}
            };
            }

            public State nextState(Command cmd)
            {
                Transition trans = new Transition(curState, cmd);
                State nextState;
                if (!transitions.TryGetValue(trans, out nextState))
                {
                    throw new Exception("Invalid Transition nextState: "+nextState+" curState: "+curState);
                }
                curState = nextState;
                return curState;
            }
        }
    }

    
}
