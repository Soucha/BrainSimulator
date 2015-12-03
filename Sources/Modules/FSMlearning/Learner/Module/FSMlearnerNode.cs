using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoodAI.Core.Memory;
using GoodAI.Core.Nodes;
using GoodAI.Core.Task;
using GoodAI.Core.Utils;
using YAXLib;

namespace MyCompany.Modules.FSMlearnerModule
{
    /// <author>Michal Soucha</author>
    /// <meta></meta>
    /// <status>TEST</status>
    /// <summary>Learning given unknown system modelled by a Finite-State Machine using interaction with the system.</summary>
    /// <description>TODO</description>
    public class FSMlearnerNode : MyWorkingNode
    {
        [MyInputBlock(0)]
        public MyMemoryBlock<int> SystemResponse
        {
            get { return GetInput<int>(0); }
        }

        [MyOutputBlock(0)]
        public MyMemoryBlock<int> QueryInputSymbol
        {
            get { return GetOutput<int>(0); }
            set { SetOutput<int>(0, value); }
        }

        public MyMemoryBlock<int> FSMtransition { get; private set; }
        public MyMemoryBlock<int> FSMoutput { get; private set; }

        [MyBrowsable, Category("Parameters")]
        [YAXSerializableField(DefaultValue = true), YAXElementFor("Parameters")]
        public bool ParameterProperty { get; set; }

        [MyBrowsable, Category("Parameters")]
        [YAXSerializableField(DefaultValue = 2)]
        public int AlphabetSize { get; set; }

        [MyBrowsable, Category("Parameters")]
        [YAXSerializableField(DefaultValue = 2)]
        public int MaxExtraDepth { get; set; }

        public int numStates = 0;

        public override void UpdateMemoryBlocks()
        {
            QueryInputSymbol.Count = 1;

            FSMtransition.Count = (numStates + 1) * (AlphabetSize + 1);
            FSMtransition.ColumnHint = (AlphabetSize + 1);
            FSMoutput.Count = (numStates + 1) * 2;
            FSMoutput.ColumnHint = 2;

        }

        public FSMlearnerTask LearningTask { get; private set; }
    }

    /// <summary>
    /// Learning task.
    /// </summary>
    public class FSMlearnerTask : MyTask<FSMlearnerNode>
    {
        private class DictComp : IEqualityComparer<ArrayList>
        {

            public bool Equals(ArrayList x, ArrayList y)
            {
                if (x.Count != y.Count) return false;
                for (int i = 0; i < x.Count; i++)
                {
                    if ((int)x[i] != (int)y[i]) return false;
                }
                return true;
            }

            public int GetHashCode(ArrayList obj)
            {
                return obj.Count;
            }
        }
        private Dictionary<ArrayList, ArrayList> table;
        private ArrayList sEntries;
        private Dictionary<ArrayList, int> eEntries;
        private Queue<ArrayList> queries;
        private ArrayList tests;

        private int queryPtr;
        private bool waitingForResponse;

        public override void Init(int nGPU)
        {
            table = new Dictionary<ArrayList, ArrayList>(new DictComp());
            sEntries = new ArrayList();
            sEntries.Add(new ArrayList());
            eEntries = new Dictionary<ArrayList, int>(new DictComp());
            eEntries[new ArrayList()] = 0;

            queries = new Queue<ArrayList>();

            // query the initial state and its next states
            queries.Enqueue(new ArrayList());
            table[new ArrayList()] = new ArrayList();
            Owner.FSMoutput.SafeCopyToHost();
            Owner.FSMoutput.Host[0] = -1;
            Owner.FSMoutput.Host[1] = -1;
            Owner.FSMoutput.SafeCopyToDevice();
            Owner.FSMtransition.SafeCopyToHost();
            Owner.FSMtransition.Host[0] = -1;
            for (int i = 0; i < Owner.AlphabetSize; i++)
            {
                ArrayList l = new ArrayList() { -1, i };
                queries.Enqueue(l);
                ArrayList tl = new ArrayList() { i };
                table[tl] = new ArrayList() { -1 };
                Owner.FSMtransition.Host[i + 1] = i;
            }
            Owner.FSMtransition.SafeCopyToDevice();
            waitingForResponse = true;
            queryPtr = -1;

            tests = new ArrayList() { new ArrayList() };
        }

        public override void Execute()
        {
            query();
            if (!waitingForResponse)
            {
                //printTable();
                learn();
                query();
            }
        }

        private void printTable()
        {
            MyLog.INFO.WriteLine("Table " + table.Count + "x" + eEntries.Count);
            foreach (ArrayList state in table.Keys)
            {
                foreach (int input in state)
                {
                    MyLog.INFO.Write(input);
                }
                MyLog.INFO.Write("\t");
                foreach (int output in table[state])
                {
                    MyLog.INFO.Write(output + " ");
                }
                MyLog.INFO.WriteLine();
            }
        }

        private void query()
        {
            if (waitingForResponse)
            {
                Owner.SystemResponse.SafeCopyToHost();
                if (Owner.SystemResponse.Host[0] != -1)
                {
                    saveResponse(Owner.SystemResponse.Host[0]);
                    if (queryPtr < queries.Peek().Count - 1)
                    {
                        queryPtr++;
                    }
                    else
                    {
                        queries.Dequeue();
                        queryPtr = 0;
                        if (queries.Count == 0)
                        {
                            waitingForResponse = false;
                            return;
                        }
                    }
                    Owner.QueryInputSymbol.SafeCopyToHost();
                    Owner.QueryInputSymbol.Host[0] = (int)queries.Peek()[queryPtr];
                    Owner.QueryInputSymbol.SafeCopyToDevice();
                }
            }
            else if (queries.Count != 0)
            {
                Owner.QueryInputSymbol.SafeCopyToHost();
                Owner.QueryInputSymbol.Host[0] = (int)queries.Peek()[queryPtr];
                Owner.QueryInputSymbol.SafeCopyToDevice();
                waitingForResponse = true;
            }
        }

        private void saveResponse(int response)
        {
            if (queryPtr == -1) // the initial state
            {
                table[queries.Peek()].Add(response);
            }
            else if ((int)queries.Peek()[queryPtr] == -1) // reset
            {
                queries.Peek().RemoveAt(queryPtr);
                queryPtr--;
            }
            else
            {
                for (int i = 0; i <= queryPtr; i++)
                {
                    ArrayList al = queries.Peek().GetRange(0, i);
                    ArrayList dl = queries.Peek().GetRange(i, queryPtr - i + 1);
                    if (table.ContainsKey(al))
                    {
                        if (eEntries.ContainsKey(dl))
                        {
                            table[al][eEntries[dl]] = response;
                        }
                    }
                    else break;
                }
                ArrayList all = queries.Peek().GetRange(0, queryPtr + 1);
                if (table.ContainsKey(all))
                {
                    table[all][eEntries[new ArrayList()]] = response;
                }
            }
        }

        private void learn()
        {
            conjectureFSM();

            ArrayList newStates = new ArrayList();
            foreach (ArrayList stateOrig in sEntries)
            {
                for (int i = 0; i < Owner.AlphabetSize; i++)
                {
                    ArrayList nextState = new ArrayList(stateOrig);
                    nextState.Add(i);
                    bool closed = false;
                    foreach (ArrayList state in sEntries)
                    {
                        if (!isDistinguished(nextState, state))
                        {
                            closed = true;
                        }
                    }
                    if (!closed)
                    {
                        foreach (ArrayList state in newStates)
                        {
                            if (!isDistinguished(nextState, state))
                            {
                                closed = true;
                            }
                        }
                        if (!closed)
                            newStates.Add(nextState);
                    }
                }
            }
            if (newStates.Count != 0) // unclosed
            {
                foreach (ArrayList newState in newStates)
                {
                    for (int i = 0; i < Owner.AlphabetSize; i++)
                    {
                        ArrayList tl = new ArrayList(newState);
                        tl.Add(i);
                        ArrayList val = new ArrayList(eEntries.Count);
                        foreach (ArrayList test in eEntries.Keys)
                        {
                            val.Add(-1);
                            ArrayList l = new ArrayList() { -1 };
                            l.AddRange(newState);
                            l.Add(i);
                            l.AddRange(test);
                            queries.Enqueue(l);
                        }
                        table[tl] = val;
                    }
                    sEntries.Add(newState);
                }
            }
            else if (((ArrayList)tests[0]).Count >= Owner.MaxExtraDepth)
            {
                MyLog.INFO.WriteLine("Learning finished.");
            }
            else // incremental testing
            {
                //conjectureFSM();

                updateTests();
                // test it
                ArrayList val = new ArrayList(tests.Count);
                foreach (ArrayList test in tests)
                {
                    eEntries[test] = eEntries.Count;
                    val.Add(-1);
                }
                foreach (ArrayList state in table.Keys)
                {
                    table[state].AddRange(val);
                    foreach (ArrayList test in tests)
                    {
                        ArrayList l = new ArrayList() { -1 };
                        l.AddRange(state);
                        l.AddRange(test);
                        queries.Enqueue(l);
                    }
                }
            }
        }

        private void conjectureFSM()
        {
            Owner.numStates = sEntries.Count;
            Owner.FSMoutput.FreeDevice();
            Owner.FSMoutput.FreeHost();
            Owner.FSMtransition.FreeDevice();
            Owner.FSMtransition.FreeHost();
            Owner.UpdateMemoryBlocks();
            Owner.FSMoutput.SafeCopyToHost();
            Owner.FSMoutput.Host[0] = -1;
            Owner.FSMoutput.Host[1] = -1;
            Owner.FSMtransition.SafeCopyToHost();
            Owner.FSMtransition.Host[0] = -1;
            for (int i = 0; i < Owner.AlphabetSize; i++)
            {
                Owner.FSMtransition.Host[i + 1] = i;
            }
            for (int i = 0; i < sEntries.Count; i++)
            {
                Owner.FSMoutput.Host[(i + 1) * 2] = i;
                Owner.FSMoutput.Host[(i + 1) * 2 + 1] =
                    (int)table[(ArrayList)sEntries[i]][0];
                Owner.FSMtransition.Host[(i + 1) * (Owner.AlphabetSize + 1)] = i;
                for (int j = 0; j < Owner.AlphabetSize; j++)
                {
                    ArrayList nextState = new ArrayList((ArrayList)sEntries[i]);
                    nextState.Add(j);
                    for (int k = 0; k < sEntries.Count; k++)
                    {
                        if (!isDistinguished(nextState, (ArrayList)sEntries[k]))
                        {
                            Owner.FSMtransition.Host[(i + 1) * (Owner.AlphabetSize + 1) + j + 1] = k;
                            break;
                        }
                    }
                }
            }
            Owner.FSMoutput.SafeCopyToDevice();
            Owner.FSMtransition.SafeCopyToDevice();
        }

        private void updateTests()
        {
            ArrayList newTests = new ArrayList(tests.Count * Owner.AlphabetSize);
            foreach (ArrayList test in tests)
            {
                for (int i = 0; i < Owner.AlphabetSize; i++)
                {
                    ArrayList t = new ArrayList(test);
                    t.Add(i);
                    newTests.Add(t);
                }
            }
            tests = newTests;
        }

        private bool isDistinguished(ArrayList nextState, ArrayList state)
        {
            for (int i = 0; i < eEntries.Count; i++)
            {
                if ((int)table[nextState][i] != (int)table[state][i]) return true;
            }
            return false;
        }
    }
}
