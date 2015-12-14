

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
       // public manager(object boss, int managerLevel,int xPos, int yPos)
         public manager(object boss, int managerLevel)   
        {           
            bossHolder = (Boss<int[],int[]>)boss;
            currManLevel = managerLevel;
            scaledTO = currManLevel;
            time = bossHolder.timeOut / scaledTO;
            Debug.Assert(bossHolder.managerLevels >= managerLevel);
            isBase = (managerLevel == bossHolder.managerLevels);

            if(!isBase)
                 nextManager = new manager(boss, managerLevel +1);
            else  //If this is the final(base level), then the manager cannot have a * command
                for (int i = 0; i < bossHolder.AvailAct.Count - 1; i++)
                    baseAvailActions.Add(bossHolder.AvailAct[i]);
            


            //Choose a RLMETHOD to create
            if (bossHolder.RLMETHOD)
                learnMB();
            else
                learnQ();       
        }


        //BEGIN FUNCTIONS

        ////Needs to take the command given by the upper level(parameter) and use it to reference the appropriate
        //table

        public void command(int[] comd)
        {
            cmdIntSuper = comd;          
        }
         
        
        int[] getCmdStatePair()
        {
            getAgentLocalLocation();

            int[] cSPair = new int[4] { 0, 0, 0, 0, };

                cSPair[0] = cmdIntSuper[0];
                cSPair[1] = cmdIntSuper[1];

            if (!isBase)
            {
                cSPair[2] = agentLocalLocation[0];
                cSPair[3] = agentLocalLocation[1];
            }
            else
            {
                cSPair[2] = bossHolder.currAgentState[0];
                cSPair[3] = bossHolder.currAgentState[1];
            }
            
            return cSPair;
        }


        //TODO: IMPLEMENT SYSTEM USING FIRST TWO DIGITS OF STATE
        void pickCommand()
        {
            int[] actionHolder;
            //choose a value(ActionValue tables return a table of actions)
            if (isBase)
            {
               // Console.WriteLine("BseLVL cSPair: " + cmdStatePairOrigin[0] + cmdStatePairOrigin[1] + cmdStatePairOrigin[2] + cmdStatePairOrigin[3]);
                if (bossHolder.lastTrans != null)
                    bossHolder.valuesReturn = learnMethodArray.value(cmdStatePairOrigin, baseAvailActions);
                else bossHolder.valuesReturn = new double[] { 0.0, 0.0, 0.0, 0.0 };
                cmdIntMinor = bossHolder.lastAction;
                return;
            }

            else
            {
                double[] givenValue = learnMethodArray.value(cmdStatePairOrigin, bossHolder.AvailAct);
                actionHolder = bossHolder.chosenPolicy.selectAction(bossHolder.AvailAct, givenValue.ToList());
            }

            cmdIntMinor = actionHolder;
            cmdStatePairDesired[0] = cmdStatePairOrigin[0] - actionHolder[0];
            cmdStatePairDesired[1] = cmdStatePairOrigin[1] - actionHolder[1];           
        }

        //DONE NEW CMD,STATE
        void learnQ()
        {
            int[] dummy = { 1, 1, 1, 1 };

            if (isBase)
                learnMethodArray = (new ModelFreeValue<int[], int[]>(bossHolder.StateCompare,bossHolder.AC,baseAvailActions,dummy));
            else
                learnMethodArray = (new ModelFreeValue<int[], int[]>(bossHolder.StateCompare, bossHolder.AC, bossHolder.AvailAct, dummy));
        }

        //DONE NEW CMD,STATE
        void learnMB()
        {
            int[] dummy = { 1, 1, 1, 1 };

            if (isBase)
               learnMethodArray = (new ModelBasedValue<int[], int[]>(bossHolder.StateCompare, bossHolder.AC,baseAvailActions, dummy));
            else
                learnMethodArray = (new ModelBasedValue<int[], int[]>(bossHolder.StateCompare, bossHolder.AC,bossHolder.AvailAct, dummy));

        }


        //Is called after the world has made its move, and now it is time for the managers to be rewarded, recursively is called
        //Will only reward managers on decisions based upon their decisions being followed, 
        //If a manager makes an impossible decision, it will have to punish itself in its run, when the timer runs out.
        public void reward(double rwrd)
        {
            cumRwrd += bossHolder.envRwrd;

            cmdStatePairResult = getCmdStatePair();

            if (isBase)
            {
                cmdIntMinor = bossHolder.lastAction;
                rwrd += bossHolder.lastTrans.reward;
                StateTransition<int[], int[]> trans = new StateTransition<int[], int[]>(cmdStatePairOrigin, cmdIntMinor, rwrd, cmdStatePairResult);
                learnMethodArray.update(trans);
            }
            else
            {

                if (cmdStatePairResult == cmdStatePairDesired)//Went where Desired
                {
                    rwrd += cumRwrd;
                    cumRwrd = 0;
                    StateTransition<int[], int[]> trans = new StateTransition<int[], int[]>(cmdStatePairOrigin, cmdIntMinor, rwrd, cmdStatePairResult);
                    learnMethodArray.update(trans);
                    nextManager.reward(bossHolder.managerRewards);
                }
                else
                    if (cmdStatePairOrigin != cmdStatePairResult)//The result and origin are different, hence it has moved
                { nextManager.reward(-bossHolder.managerRewards); cumRwrd = 0; }

                else
                    nextManager.reward(0);
            }    
        }



        //Run is used for calling giving the next manager level a command and then moving onto that manager for repeating the process till the base
        //The base then uses pickCommand where the table of values will be given to the agent, and it will decide a move

        public void run()
        {
            
            if (isBase)
            {
                cmdStatePairOrigin = getCmdStatePair();
                pickCommand();
                return;
            }

           if(cmdStatePairOrigin != cmdStatePairResult)//It has changed positions
            {
                cmdStatePairOrigin = getCmdStatePair();
                pickCommand();
                time = bossHolder.timeOut / scaledTO;
                nextManager.command(cmdIntMinor);
            }
            
                
            if (time-- <= 0 )
            {
                StateTransition<int[], int[]> trans = new StateTransition<int[], int[]>(cmdStatePairOrigin, cmdIntMinor, -bossHolder.managerRewards, cmdStatePairDesired);
                learnMethodArray.update(trans);          
                                                               
                time = bossHolder.timeOut / scaledTO;
                pickCommand();
                nextManager.command(cmdIntMinor);          
            }
            
           // Console.WriteLine("ManagerLeveL: " + currManLevel);
           // Console.WriteLine("Uses command: " + cmdIntMinor[0] + "," + cmdIntMinor[1] + " on Child: " + cmdStatePairOrigin[2]+','+cmdStatePairOrigin[3]);
            nextManager.run();
 
        }

        //Will convert the Agents Global position to one of the positions the manager would recognize. 
        void getAgentLocalLocation()
        {
            int n = ((bossHolder.managerLevels - currManLevel));
            int diff = 2;
            for(int i=1;i<n;i++)
                diff *= 2;           
            agentLocalLocation[0] = ((bossHolder.currAgentState[0]) /diff);
            agentLocalLocation[1] = ((bossHolder.currAgentState[1]) /diff);
        }


        //MEMBERS
        MultiResolutionRL.ValueCalculation.ActionValue<int[], int[]> learnMethodArray;
             
        Boss<int[],int[]> bossHolder;
        manager nextManager;

        int[] cmdStatePairOrigin = new int[4]; //Where the Agent started
        int[] cmdStatePairResult = new int[4]; //Where did the Agent Go
        int[] cmdStatePairDesired = new int[4];//Where should the Agent have gone
        int[] cmdIntSuper = new int[2]; //Comand given in (x,y) for a move direction from the super-manager
        int[] cmdIntMinor = new int[2]; //Command given in(x,y) for a move direction for the sub-manager

        int currManLevel;
        int[] agentLocalLocation = new int[2];
        int time;
        int scaledTO = 1;
        double cumRwrd = 0;

        //Used by the base Manager 
        bool isBase = false;
        List<int[]> baseAvailActions = new List<int[]>();
    }
}
