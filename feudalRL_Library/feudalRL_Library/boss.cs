using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiResolutionRL;
using MultiResolutionRL.ValueCalculation;

namespace feudalRL_Library
{

    public class Boss : ActionValue<int[], int[]>
    {
        public Boss(IEqualityComparer<int[]> StateComparer, IEqualityComparer<int[]> ActionComparer
            , List<int[]> AvailableActions, int[] StartState, params object[] parameters)
          : base(StateComparer, ActionComparer, AvailableActions, StartState)
        {
            //Boss will have one child, This child will be the first manager, that then has 4 children of its own.
            SC = StateComparer;
            AC = ActionComparer;
            AvailAct = AvailableActions;
            AvailAct.Add(new int[2] { 0, 0 });



            RLMETHOD = (bool)parameters[1]; // 0 for QL, 1 For MB
            alpha = (double)parameters[2];
            gamma = (double)parameters[3];
            timeOut = (int)parameters[4]; //Number of runs before a manager will force a sub-manager to stop
            managerRewards = (double)parameters[5]; // Amount a manager will reward its sub-manager for completing a command
            chosenPolicy = (MultiResolutionRL.Policy<int[], int[]>)parameters[6];

            managerLevels = (int)Math.Log((int)parameters[0], 4); //The depth of the RL system

            child = new manager(this, 0, 1, 1);

        }

        //Run function will reward and command sub manager
        //Will be the method called to iterate an action/move
        //Will Need to Reward if the environment Gives a reward as well
        void Run()
        {

            child.command('*');
                child.run();   
        }


        public override double[] value(int[] state, List<int[]> actions)
        {
            Run();
            return valuesReturn;
        }

        public override void update(MultiResolutionRL.StateTransition<int[], int[]> transition)
        {
            envRwrd = transition.reward;
            currAgentState = transition.newState;
        }

        public override int[] PredictNextState(int[] state, int[] action)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<int[], double> PredictNextStates(int[] state, int[] action)
        {
            throw new NotImplementedException();
        }

        public override double PredictReward(int[] state, int[] action, int[] newState)
        {
            throw new NotImplementedException();
        }



        public int managerLevels;
        public double managerRewards;
        public int timeOut;
        public double alpha;
        public double gamma;
        public bool RLMETHOD;
        public double[] valuesReturn;
        public int[] currAgentState;
        public MultiResolutionRL.Policy<int[], int[]> chosenPolicy;
        public IEqualityComparer<int[]> SC;
        public IEqualityComparer<int[]> AC;
        public List<int[]> AvailAct;
        public double envRwrd;

        feudalRL_Library.manager child;
       


    }

}



