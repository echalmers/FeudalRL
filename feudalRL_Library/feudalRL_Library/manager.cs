

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MultiResolutionRL;
using MultiResolutionRL.ValueCalculation;


namespace feudalRL_Library
{
    

    public class manager
    {
        public manager(Boss boss, int managerLevel,int xPos, int yPos)
            
        {
            
            bossHolder = boss;
            currManLevel = managerLevel;
            time = 0;
            Debug.Assert(bossHolder.managerLevels >= managerLevel);
            isBase = (managerLevel == bossHolder.managerLevels);


            //If this is the final(base level), then the manager cannot have a * command and wont have children
            if (isBase)
            {
                for (int i = 0; i < boss.AvailAct.Count - 1; i++)
                    baseAvailActions.Add(boss.AvailAct[i]);
            }

            else //Create the children, they will be index 0 1 2 3, Clockwise, from bottom Left corner
            {
                for (int i = 0; i < 4; i++)
                {
                    childrenX[i] = xPos * 2 - XYSHIFT[1][i];
                    childrenY[i] = yPos * 2 - XYSHIFT[2][i];
                    children[i] = new manager(boss, managerLevel + 1, childrenX[i], childrenY[i]);
                }
                //Add Meta children on border, for checking when agent has left the control of manager, from Bottom Left Left clockwise
                for (int i = 4; i < 12; i++)
                {
                    childrenX[i] = xPos * 2 - XYSHIFT[1][i];
                    childrenY[i] = yPos * 2 - XYSHIFT[2][i];
                }
            }
            //Choose a RLMETHOD to create
            if (bossHolder.RLMETHOD)
                learnMB();
            else
                learnQ();       
        }


        //BEGIN FUNCTIONS

        ////Needs to take the command given by the upper level(parameter) and use it to reference the appropriate
        //table
        public void command(char comd)
        {
            switch(comd)
            {
                case '*': activeArray = 4; return;
                case 'N': activeArray = 0; return;
                case 'S': activeArray = 1; return;
                case 'E': activeArray = 2; return;
                case 'W': activeArray = 3; return;
                default: bool commandFailed = false; Debug.Assert(commandFailed); break;
            }
        }
        void pickCommand()
        {
            int[] actionHolder;

            //choose a value(ActionValue tables return a table of actions)
           
            
            if (isBase)//If it is the base, then the manager needs to send the table to the boss
            {
                bossHolder.valuesReturn = learnMethodArray[activeArray].value(activeChild, baseAvailActions);
                return;
            }
            else
            {
                double[] givenValue = learnMethodArray[activeArray].value(activeChild, bossHolder.AvailAct);
                actionHolder = bossHolder.chosenPolicy.selectAction(bossHolder.AvailAct, givenValue.ToList());
            }


            if (activeChild >= 4)
            {
                bool theACTIVECHILDisHIGH = false;
                Debug.Assert(theACTIVECHILDisHIGH);
            };
            switch (actionHolder[0])
            {
                case -1: cmd = 'W'; switch(activeChild)
                    {
                        case 0: goalState = 4; break;
                        case 1: goalState = 5; break;
                        case 2: goalState = 1; break;
                        case 3: goalState = 0; break;
                    }
                    break;

                case 0:
                    {
                        switch (actionHolder[1])
                        {
                            case 1: cmd = 'N';
                                switch (activeChild)
                                {
                                    case 0: goalState = 1; break;
                                    case 1: goalState = 6; break;
                                    case 2: goalState = 7; break;
                                    case 3: goalState = 2; break;
                                } break;
                            case 0: cmd = '*';
                                switch (activeChild)
                                {
                                    case 0: goalState = 0; break;
                                    case 1: goalState = 1; break;
                                    case 2: goalState = 2; break;
                                    case 3: goalState = 3; break;
                                }
                                break;
                            case -1: cmd = 'S';
                                switch (activeChild)
                                {
                                    case 0: goalState = 11; break;
                                    case 1: goalState = 0; break;
                                    case 2: goalState = 3; break;
                                    case 3: goalState = 10; break;
                                }
                                break;
                        }
                        break;
                    }

                case 1: cmd = 'E'; switch (activeChild)
                    {
                        case 0: goalState = 3; break;
                        case 1: goalState = 2; break;
                        case 2: goalState = 8; break;
                        case 3: goalState = 9; break;
                    }
                    break;
            }
        }

        void pickChild()
        {
            //Find Agents local Location
            getAgentLocalLocation();
            for (int i = 0; i < 12; i++)
                if (agentLocalLocation[0] == childrenX[i])
                    if (agentLocalLocation[1] == childrenY[i])
                    { activeChild = i; return; }

            
            
        }

        void learnQ()
        {
            int dummy = 1;
                        
            //Create 5 Q tables corresponding to 0 = N[0,1], 1 = S[0,-1], 2 = E[1,0], 3 = W[-1,0], 4= *[0,0]
            if(isBase)
                for (int i = 0; i < 5; i++)
                    learnMethodArray.Add(new ModelFreeValue<int, int[]>(EqualityComparer<int>.Default, bossHolder.AC, baseAvailActions, dummy));
            else
            for (int i = 0; i < 5; i++)
                learnMethodArray.Add(new ModelFreeValue<int, int[]>(EqualityComparer<int>.Default,bossHolder.AC,bossHolder.AvailAct,dummy));
            

        }

        void learnMB()
        {
            int dummy = 1;

            //Create 5 MB tables corresponding to 0 = N[0,1], 1 = S[0,-1], 2 = E[1,0], 3 = W[-1,0], 4= *[0,0]
            if (isBase)
                for (int i = 0; i < 5; i++)
                    learnMethodArray.Add(new ModelBasedValue<int, int[]>(EqualityComparer<int>.Default, bossHolder.AC, baseAvailActions, dummy));
            else
            for (int i = 0; i < 5; i++)
                learnMethodArray.Add(new ModelBasedValue<int, int[]>(EqualityComparer<int>.Default, bossHolder.AC, bossHolder.AvailAct, dummy));

        }

        int[] cmdToint(char cmd)
        {
            switch (cmd)
            {
                case '*': return new int[] { 0, 0 };
                case 'N': return new int[] { 0, 1 };
                case 'S': return new int[] { 0, -1 };
                case 'E': return new int[] { 1, 0 };
                case 'W': return new int[] { -1, 0 };
                default: bool badCMDtoInt = false; Debug.Assert(badCMDtoInt); return new int[] { 999, 999 };
            }
        }

        public void reward(double rwrd=0)
        {
            int oState = activeChild;
            Debug.Assert(oState <= 3);
            pickChild();
            int newstate = activeChild;
            Debug.Assert(activeChild <= 11);
            int[] cmdint = cmdToint(cmd);
                                 
            rwrd += bossHolder.envRwrd;   
            if (oState == goalState)//Deal with *
                if (oState == newstate)
                    rwrd += 0;
                else rwrd += -(bossHolder.managerRewards);//Left the state on * cmd

            else if (newstate == goalState)//Was not * command, went to cmd'd state
                rwrd += bossHolder.managerRewards;
                else   //Went to the wrong state, will make a negative reward
                rwrd += -(bossHolder.managerRewards);
            

            StateTransition<int, int[]> trans = new StateTransition<int, int[]>(oState, cmdint, rwrd, newstate);
            children[activeChild].learnMethodArray[activeArray].update(trans);

        }

       public void run()
        {
            //Need to determine base case, where the manager will choose a state instead of a child. 
            if (isBase)
            {
                pickCommand();
                return;
            }

            Debug.Assert(time > -2);
            if(time-- == 0)//When time reaches zero, need to pick a new command
            {

                time = bossHolder.timeOut;//Reset timer 
                pickChild();
                pickCommand();          
                children[activeChild].command(cmd);
              
                //IF the star action is called, the child will keep looking around inside the state
                //This will continue untill time out, If the child left the state then they should be punished                                   
            }
            children[activeChild].run();
            children[activeChild].reward();
        }

        //Will convert the Agents Global position to one of the positions the manager would recognize. 
        void getAgentLocalLocation()
        {

            int diff = ((bossHolder.managerLevels - currManLevel) * 2);
            agentLocalLocation[0] = bossHolder.currAgentState[0] /diff;
            agentLocalLocation[1] = bossHolder.currAgentState[1] /diff;

        }


        //MEMBERS
        List<MultiResolutionRL.ValueCalculation.ActionValue<int, int[]>> learnMethodArray;
        
      
        Boss bossHolder;
       
        manager[] children;
        int[] childrenX = new int[13], childrenY = new int[13];
        
        int activeChild;
        int activeArray;
        int goalState;
        

        int[][] XYSHIFT = new int[2][] { new int[12] { 1, 1, 0, 0,-1,-1,0,1,2,2,1,0 }, new int[12] { 1, 0, 0, 1,0,1,2,2,1,0,-1,-1 } };
        int xGoal, yGoal;
        int currManLevel;
        int[] agentLocalLocation;
        char cmd = 'N';
        int time;
        int xStart, yStart;
        bool isWorldReward;

        //Used by the base Manager 
        bool isBase;
        List<int[]> baseAvailActions;

    }
}
