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

        [MyTaskGroup("Mode")]
        public FSMlearnerTask LearningTask { get; private set; }
        [MyTaskGroup("Mode")]
        public FSMlearnerTaskDummy LearningTaskOld { get; private set; }
    }

    /// <summary>
    /// Learning task.
    /// </summary>
    public class FSMlearnerTask : MyTask<FSMlearnerNode>
    {
        private class Node
        {
            public ArrayList succ;
            public int output = -1;
            public int state = -1;
            public int refNode = -1;
            public ArrayList refNodes;

            public int parentNode, parentInput;
            public int position;
            public int numTests = 0;


            public Node(int parent, int position, int input)
            {
                this.parentNode = parent;
                this.parentInput = input;
                this.position = position;
                this.refNodes = new ArrayList();
            }
        }

        private ArrayList op;
        private ArrayList states;
        private ArrayList oldTests, tests;
        private int testedState;
        private int testedSequence;
        private bool waitingForResponse;
        private ArrayList tmpSucc;
        private int currentNode;
        private int queryInput;

        public override void Init(int nGPU)
        {
            Node root = new Node(-1, 0, -1);
            root.refNode = 0;
            root.state = 0;
            root.succ = new ArrayList(Owner.AlphabetSize);
            root.numTests = Owner.AlphabetSize;
            op = new ArrayList() { root };
            states = new ArrayList() { 0 };
            oldTests = new ArrayList();
            tests = new ArrayList(Owner.AlphabetSize);
            tmpSucc = new ArrayList(Owner.AlphabetSize);
            for (int i = 0; i < Owner.AlphabetSize; i++)
            {
                ((Node)op[0]).succ.Add(createNewNode(0, i));
                tests.Add(new ArrayList() { i });
                tmpSucc.Add(-1);
            }
            testedSequence = -1;
            testedState = 0;
            updateTests();

            currentNode = 0;
            queryInput = -1;
            waitingForResponse = true;
        }

        public override void Execute()
        {
            if (tests.Count == 0)
            {
                MyLog.INFO.WriteLine("Learning finished.");
            }
            else
            {
                learn();
            }
        }

        private int createNewNode(int parent, int input)
        {
            Node n = new Node(parent, op.Count, input);
            n.succ = new ArrayList(tmpSucc);
            op.Add(n);
            return n.position;
        }

        private bool addTest()
        {
            testedSequence++;
            if (testedSequence == tests.Count)
            {
                testedSequence = 0;
                testedState++;
                if (testedState == states.Count)
                {
                    if (((ArrayList)tests[0]).Count >= Owner.MaxExtraDepth)
                    {
                        oldTests.AddRange(tests);
                        tests.Clear();
                        return false;
                    }
                    updateTests();
                    testedState = 0;
                }
            }
            int node = (int)states[testedState];
            var seq = (ArrayList)tests[testedSequence];
            int addedTests = addSequence(node, seq, true);
            if (addedTests != 0)
            {
                updateNumTests(node, 0, addedTests);
            }
            else // test have been processed
            {
                return addTest();
            }
            return true;
        }

        private void updateNumTests(int node, int terminateNode, int addedTests)
        {
            while (node != terminateNode)
            {
                int parent = ((Node)op[node]).parentNode;
                ((Node)op[parent]).numTests += addedTests;
                node = parent;
            }
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
            oldTests.AddRange(tests);
            tests = newTests;
        }

        private void learn()
        {
            if (waitingForResponse)
            {
                Owner.SystemResponse.SafeCopyToHost();
                if (Owner.SystemResponse.Host[0] != -1)
                {
                    saveResponse(Owner.SystemResponse.Host[0]);
                    waitingForResponse = false;
                    learn();
                }
            }
            else if ((((Node)op[0]).numTests > 0) || addTest())
            {
                query();
                waitingForResponse = true;
            }
        }

        private void query()
        {
            if (((Node)op[currentNode]).numTests != 0)
            {
                for (int i = 0; i < Owner.AlphabetSize; i++)
                {
                    if (((int)((Node)op[currentNode]).succ[i] != -1) && (
                        ((int)((Node)op[(int)((Node)op[currentNode]).succ[i]]).output == -1) ||
                        ((int)((Node)op[(int)((Node)op[currentNode]).succ[i]]).numTests != 0)))
                    {
                        queryInput = i;
                        break;
                    }
                }
            }
            else
            {
                queryInput = -1;
            }
            Owner.QueryInputSymbol.SafeCopyToHost();
            Owner.QueryInputSymbol.Host[0] = queryInput;
            Owner.QueryInputSymbol.SafeCopyToDevice();
        }

        private void saveResponse(int response)
        {
            if (queryInput == -1) {// reset, i.e. output of the initial state
                ((Node)op[0]).output = response;
                currentNode = 0;
                return;
            }
            
            // check successor
            int nextNode = (int)((Node)op[currentNode]).succ[queryInput];
            if (((Node)op[nextNode]).output == -1) // queried for the first time
            {
                ((Node)op[nextNode]).output = response;
                for (int state = 0; state < states.Count; state++)
                {
                    if (((Node)op[(int)states[state]]).output == response)
                    {
                        ((Node)op[nextNode]).refNodes.Add(states[state]);
                    }
                }
                if (((Node)op[nextNode]).refNodes.Count == 0) // new state
                {
                    addNewState(nextNode);
                } 
                else if (((Node)op[nextNode]).refNodes.Count == 1) // new state
                {
                    ((Node)op[nextNode]).refNode = (int)((Node)op[nextNode]).refNodes[0];
                }
                
                // check current Node
                checkNode(currentNode, queryInput);

            }
            currentNode = nextNode;
            if (((Node)op[currentNode]).numTests == 0)
            {
                updateNumTests(currentNode, 0, -1);
            }
        }

        private void checkNode(int node, int queryInput)
        {
            if (((Node)op[node]).position == ((Node)op[node]).refNode) // state
            {
                return;
            }
            bool checkPrev = false;
            for (int i = ((Node)op[node]).refNodes.Count-1; i >= 0; i--)
			{
                if (isDistinguished(node, (int)((Node)op[node]).refNodes[i], queryInput)) {
                    ((Node)op[node]).refNodes.RemoveAt(i);
                    checkPrev = true;
                }
			}
            if (((Node)op[node]).refNodes.Count == 0) // new state
            {
                addNewState(node);
            }
            else if (((Node)op[node]).refNodes.Count == 1)
            {
                ((Node)op[node]).refNode = (int)((Node)op[node]).refNodes[0];
            }
            if (checkPrev)
            {
                checkNode(((Node)op[node]).parentNode, ((Node)op[node]).parentInput);
            }
        }

        private bool isDistinguished(int node, int refNode, int input)
        {
            int nextNode = (int)((Node)op[refNode]).succ[input];
            if (nextNode == -1) return false;
            if (((Node)op[node]).output != ((Node)op[nextNode]).output) return true;
            // TODO
            return false;
        }

        private void addNewState(int node)
        {
            ((Node)op[node]).state = states.Count;
            ((Node)op[node]).refNode = node;
            states.Add(node);

            int addedTests = 0;
            foreach (ArrayList seq in oldTests)
            {
                addedTests += addSequence(node, seq, true);    
            }
            updateNumTests(node, 0, addedTests);
        }

        private int addSequence(int node, ArrayList seq, bool withTests)
        {
            int baseNode = node;
            int addedTests = 0;
            bool incNumTests = false;
            for (int i = 0; i < seq.Count; i++)
            {
                int next = (int)((Node)op[node]).succ[(int)seq[i]];
                if (next == -1)
                {
                    next = createNewNode(node, (int)seq[i]);
                    ((Node)op[node]).succ[(int)seq[i]] = next;
                    if ((((Node)op[node]).numTests > 0) || (incNumTests))
                    {
                        addedTests = 1;
                        updateNumTests(node, baseNode, addedTests);
                        incNumTests = false;
                    }
                    ((Node)op[node]).numTests++;
                }
                else if (((Node)op[next]).numTests == 0)
                {
                    incNumTests = true;
                }
                node = next;
            }
            /*
            if (withTests) {
            foreach (var testSeq in hsi[reachedState])
            {
                addedTests += addSequence(node, testSeq, false);
            }
            updateNumTests(node, baseNode, ((Node)op[node]).numTests);
            }
             */

            return addedTests;
        }
    }

    /// <summary>
    /// Learning task.
    /// </summary>
    public class FSMlearnerTaskDummy : MyTask<FSMlearnerNode>
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
                int lastPosResponse = -2;
                for (int i = 0; i <= queryPtr; i++)
                {
                    ArrayList al = queries.Peek().GetRange(0, i);
                    ArrayList dl = queries.Peek().GetRange(i, queryPtr - i + 1);
                    if (table.ContainsKey(al))
                    {
                        if (eEntries.ContainsKey(dl))
                        {
                            table[al][eEntries[dl]] = response;
                            if ((queryPtr == queries.Peek().Count - 1) && (eEntries[dl] == eEntries.Count - 1))
                            {
                                conjectureFSM();
                            }
                        }
                        if ((int)table[al][eEntries[new ArrayList()]] >= 0)
                        {
                            lastPosResponse = (int)table[al][eEntries[new ArrayList()]];
                        }
                    }
                    else break;
                }
                ArrayList all = queries.Peek().GetRange(0, queryPtr + 1);
                if (table.ContainsKey(all))
                {
                    table[all][eEntries[new ArrayList()]] = (response == -2) ? lastPosResponse : response;
                }
            }
        }

        private void learn()
        {
            //conjectureFSM();

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
                    conjectureFSM();
                }
            }
            else if (((ArrayList)tests[0]).Count >= Owner.MaxExtraDepth)
            {
                MyLog.INFO.WriteLine("Learning finished.");
            }
            else // incremental testing
            {
                conjectureFSM();

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
            string dotText = "digraph {" + Environment.NewLine;
            for (int i = 0; i < sEntries.Count; i++)
            {
                Owner.FSMoutput.Host[(i + 1) * 2] = i;
                int output = (int)table[(ArrayList)sEntries[i]][0];
                Owner.FSMoutput.Host[(i + 1) * 2 + 1] = output;
                dotText += i + " [label=\"" + i + "\n" + output + "\"];" + Environment.NewLine;
                Owner.FSMtransition.Host[(i + 1) * (Owner.AlphabetSize + 1)] = i;
                for (int j = 0; j < Owner.AlphabetSize; j++)
                {
                    ArrayList nextState = new ArrayList((ArrayList)sEntries[i]);
                    nextState.Add(j);
                    Owner.FSMtransition.Host[(i + 1) * (Owner.AlphabetSize + 1) + j + 1] = -1;
                    for (int k = 0; k < sEntries.Count; k++)
                    {
                        if (!isDistinguished(nextState, (ArrayList)sEntries[k]))
                        {
                            Owner.FSMtransition.Host[(i + 1) * (Owner.AlphabetSize + 1) + j + 1] = k;
                            dotText += i + " -> " + k + " [label=\"" + j + "\"];" + Environment.NewLine;
                            break;
                        }
                    }
                }
            }
            dotText += "}";
            Owner.FSMoutput.SafeCopyToDevice();
            Owner.FSMtransition.SafeCopyToDevice();
            System.IO.File.WriteAllText(@"outputDOT.gv.txt", dotText);
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
