using System;
using System.Collections.Generic;

namespace KatLib.State_Machine
{
    public class StateMachine<StateID, BState> where StateID : Enum where BState : BaseState
    {
        public readonly Dictionary<StateID, BState> States = new ();
        
        public StateID CurrentStateID { get; protected set; }
        public StateID LastStateID { get; protected set; }
        public BState CurrentState { get; protected set; }

        public void AddNewState(StateID stateID, BState newState)
        {
            States.Add(stateID, newState);
        }
        
        public virtual void Initialize(StateID startedState)
        {
            CurrentState = States[startedState];
            CurrentStateID = startedState;
            LastStateID = startedState;
            CurrentState.Enter();
        }

        public void ChangeState(StateID newStateID)
        {
            var newState = States[newStateID];
            
            if (CurrentState == newState) return;
            LastStateID = CurrentStateID;
            CurrentState.Exit();
            CurrentStateID = newStateID;
            CurrentState = newState;
            CurrentState.Enter();
        }
    }
}