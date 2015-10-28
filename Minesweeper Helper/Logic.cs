// Minesweeper Helper, by Carl Walsh May 2010
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Minesweeper_Helper
{
    //Thinks about the minesweeper gameboard. Gives suggestions, and
    //can take input about what numbers appear
    class Logic
    {
        int WIDTH, HEIGHT;
        bool USE_SIMPLE; //which logics to use
        bool USE_COMPLEX;
        Cell[,] board; //reprents squares in (x,y) format, as a num 0-8, while
                       //-1 represents an unclicked state, and -2 is a mine
        List<Point> newMines; //newly assigned mines
        List<Point> newSafes; //newly assigned safe spaces
        List<Group> groups; //all the hint information
        public Logic(int w, int h, bool useSimple, bool useComplex)
        {
            WIDTH = w;
            HEIGHT = h;
            board = new Cell[w, h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    board[x, y] = new Cell();
            USE_SIMPLE = useSimple;
            USE_COMPLEX = useComplex;
            if(USE_COMPLEX)
                groups = new List<Group>();
            newMines = new List<Point>();
            newSafes = new List<Point>();
        }
        //Gets the safe spaces and mines as a pair
        public KeyValuePair<List<Point>, List<Point>> nextMoves()
        {
            if (USE_SIMPLE)
                simpleLogic();
            if (USE_COMPLEX)
                complexLogic();

            //Reset the lists of mines and safes, but copy the information!
            KeyValuePair<List<Point>, List<Point>> ans =
                new KeyValuePair<List<Point>, List<Point>>
                    (new List<Point>(newSafes), new List<Point>(newMines));
            newMines = new List<Point>(); //reset the "new" mines
            newSafes = new List<Point>(); //and safe spaces
            return ans;
        }
        public void setNum(Point p, int num)
        {
            set(p, num); //for simple logic

            //Allow inputted mines to be put into logic
            if (USE_COMPLEX && num == -2)
            {
                inputToGroups(p, true);
                //Remove duplicate and empty groups
                processGroups();
            }

            if (USE_COMPLEX && num >= 0)
            {
                //Let each group know about the new spaces
                inputToGroups(p, false);

                //Put a new group into the logic
                List<Point> cells = nextToAndUnsure(p);
                int minesAround = mines(nextTo(p));
                if (cells.Count != 0)
                    addNewGroup(new Group(cells, num - minesAround));

                //Remove duplicate and empty groups
                processGroups();
            }
        }


        public void setNums(List<KeyValuePair<Point, int>> vals)
        {
            foreach (KeyValuePair<Point, int> x in vals)
                setNum(x.Key, x.Value);
        }

        public int[,] getBoard()
        {
            int[,] tempBoard = new int[WIDTH, HEIGHT];
            for (int x = 0; x < WIDTH; ++x)
                for (int y = 0; y < HEIGHT; ++y)
                    if(board[x, y].num == -3) //Don't copy over "not-mine"
                        tempBoard[x, y] = -1;
                    else
                        tempBoard[x, y] = board[x, y].num;
            return tempBoard;
        }
        public void printLogic()
        {
            Console.WriteLine("Logic sees:");
            if (HEIGHT > 9)
                Console.Write(' ');
            Console.Write("  ");
            for (int x = 0; x < WIDTH; ++x)
            {
                Console.Write(x);
                if (x < 10)
                    Console.Write(' ');
            }
            Console.WriteLine();
            Point p = new Point(0, 0);
            for (; p.Y < HEIGHT; ++p.Y)
            {
                Console.Write(p.Y);
                if (HEIGHT > 9 && p.Y < 10)
                    Console.Write(' ');
                Console.Write(' ');
                for (p.X = 0; p.X < WIDTH; ++p.X)
                {
                    int num = get(p);
                    if (num > 0 && num < 9)
                        Console.Write("" + num + " ");
                    else if (num == -1)
                        Console.Write("- ");
                    else
                        Console.Write("  ");
                }
                Console.WriteLine();
            }
        }

        //Looks at each cell as a hint, and will act if the cell is surrounded
        //by mines or safes
        private void simpleLogic()
        {
            //Search for all-mines or no-mines around hints
            bool madeChange = true;
            while (madeChange)
            {
                madeChange = false;
                Point p = new Point(0,0);
                for (; p.Y < HEIGHT; p.Y++)
                    for (p.X = 0; p.X < WIDTH; p.X++)
                        if (isNumber(p))
                        {
                            List<Point> cells = nextTo(p);
                            if (get(p) == mines(cells))
                            { //no more mines around this
                                foreach (Point c in cells)
                                    if (unsure(c))
                                    {
                                        setSafe(c);
                                        madeChange = true;
                                    }
                            }
                            else if (get(p) == mines(cells) + unsures(cells))
                            { //only mines around this
                                foreach(Point c in cells)
                                    if(unsure(c))
                                    {
                                        setMine(c);
                                        madeChange = true;
                                    }
                            }
                        }
            }
        }
        
        /*The collections will work as follows:
        * In minesweeper, each hint represents the spaces around it having a
        * certain number of mines. So a hint represents a collection of points
        * paired with the amount of mines there (named Group). If I add a group
        * and it is a sub-group of another group, the other group will create a
        * new smaller group that is the "opposite half" of original
        * 
        * Next I will process the group, where any group which contains as many
        * spaces as mines gets filled with all mines, and any group which 
        * contains zero mines is all no mines.
        * 
        * I will keep all the spaces I have decided on in a list, then go 
        * through every group, and removes the spaces that
        * are no longer uncertain, and update the mines remaining in the group
        */
        private void complexLogic()
        {
            bool madeChange = true;
            while (madeChange)
            {
                madeChange = false;
                List<Point> mines = new List<Point>(); //temp lists to work
                List<Point> safes = new List<Point>(); //around foreach
                foreach (Group g in groups)
                {
                    bool areMines = true;
                    List<Point> points = new List<Point>();

                    //Use any information the groups may have
                    if (g.mineInfo(ref points, ref areMines))
                    {
                        List<Point> pointCopy = new List<Point>(points);
                        madeChange = true;
                        if (areMines)
                            foreach (Point p in pointCopy)
                                mines.Add(p);
                        else
                            foreach (Point p in pointCopy)
                                safes.Add(p);
                        processGroups();
                    }
                }
                foreach (Point p in mines)
                    setMine(p);
                foreach (Point p in safes)
                    setSafe(p);
            }
        }
        private void processGroups()//removes duplicates and empty Groups
        {
            //remove all empty Groups and duplicates from groups
            List<Group> newGroups = new List<Group>();
            foreach (Group g in groups)
                if (!g.isEmpty() && !newGroups.Contains(g))
                    newGroups.Add(g);
            groups = newGroups;
        }

        //adds the point to output as mine or safe, and handles updating groups
        private void setMine(Point p)
        {
            board[p.X, p.Y].setMine();
            if(!newMines.Contains(p))
                newMines.Add(p);
            if (USE_COMPLEX)
                inputToGroups(p, true);
        }
        private void setSafe(Point p)
        {
            board[p.X, p.Y].setSafe();
            if(!newSafes.Contains(p))
                newSafes.Add(p);
            if (USE_COMPLEX)
                inputToGroups(p, false);
        }
        private void inputToGroups(Point p, bool isMine)//handles updating groups
        {
            List<Group> tempToAdd = new List<Group>();
            foreach (Group g in groups)
                if (g.inputCellAsMine(p, isMine))
                    tempToAdd.Add(g);
            foreach (Group g in tempToAdd)
                addNewGroup(g); //won't add the group, but checks sub-groups
        }

         //TODO: make the number updates handled in SystemIO, so I don't
         //parse the same numbers more than once.

        private int get(Point p)
        {
            return board[p.X, p.Y].num;
        }
        private void set(Point p, int num)
        {
            board[p.X, p.Y].setNumber(num);
        }
        private int nextToNum(Point p)//how many cells around a cell
        {
            int numEdges = 0;
            if (p.X == 0 || p.X + 1 == WIDTH)
                numEdges++;
            if (p.Y == 0 || p.Y + 1 == HEIGHT)
                numEdges++;

            if (numEdges == 0)
                return 8;
            if (numEdges == 1)
                return 5;
            return 3;
        }
        private List<Point> nextTo(Point p)//the cells around a cell
        {
            List<Point> ans = new List<Point>();
            Point P = new Point(p.X - 1, p.Y - 1);
            if (valid(P)) //start upper-left
                ans.Add(new Point(P.X, P.Y));
            P.X++;
            if (valid(P)) //upper
                ans.Add(new Point(P.X, P.Y));
            P.X++;
            if (valid(P)) //upper-right
                ans.Add(new Point(P.X, P.Y));
            P.Y++;
            if (valid(P)) //right
                ans.Add(new Point(P.X, P.Y));
            P.Y++;
            if (valid(P)) //lower-right
                ans.Add(new Point(P.X, P.Y));
            P.X--;
            if (valid(P)) //lower
                ans.Add(new Point(P.X, P.Y));
            P.X--;
            if (valid(P)) //lower-left
                ans.Add(new Point(P.X, P.Y));
            P.Y--;
            if (valid(P)) //left
                ans.Add(new Point(P.X, P.Y));
            return ans;
        }
        private List<Point> nextToAndUnsure(Point p)//unsure around a cell
        {
            List<Point> ans = new List<Point>();
            Point P = new Point(p.X - 1, p.Y - 1);
            if (valid(P) && unsure(P)) //start upper-left
                ans.Add(new Point(P.X, P.Y));
            P.X++;
            if (valid(P) && unsure(P)) //upper
                ans.Add(new Point(P.X, P.Y));
            P.X++;
            if (valid(P) && unsure(P)) //upper-right
                ans.Add(new Point(P.X, P.Y));
            P.Y++;
            if (valid(P) && unsure(P)) //right
                ans.Add(new Point(P.X, P.Y));
            P.Y++;
            if (valid(P) && unsure(P)) //lower-right
                ans.Add(new Point(P.X, P.Y));
            P.X--;
            if (valid(P) && unsure(P)) //lower
                ans.Add(new Point(P.X, P.Y));
            P.X--;
            if (valid(P) && unsure(P)) //lower-left
                ans.Add(new Point(P.X, P.Y));
            P.Y--;
            if (valid(P) && unsure(P)) //left
                ans.Add(new Point(P.X, P.Y));
            return ans;
        }
        private bool valid(Point p)
        {
            return (0 <= p.X && p.X < WIDTH) &&
                   (0 <= p.Y && p.Y < HEIGHT);
        }
        private bool isNumber(Point p)
        {
            return board[p.X, p.Y].isNumber();
        }
        private bool mine(Point p)
        {
            return board[p.X, p.Y].isMine();
        } //guarenteed to be a mine
        private int mines(List<Point> l)
        {
            int count = 0;
            foreach (Point p in l)
                if (mine(p))
                    ++count;
            return count;
            //return areEqual(l, -2);
        }
        private bool notMine(Point p)
        {
            return !(mine(p) || unsure(p));
        } //guarenteed to be not a mine
        private int notMines(List<Point> l)
        {
            return l.Count - mines(l) - unsures(l);
        }
        private bool unsure(Point p)
        {
            return board[p.X, p.Y].isUnsure();
        }
        private int unsures(List<Point> l)
        {
            int count = 0;
            foreach (Point p in l)
                if (unsure(p))
                    ++count;
            return count;
            //return areEqual(l, -1);
        }
        //private int areEqual(List<Point> l, int eq)
        //{
        //    int ans = 0;
        //    foreach (Point p in l)
        //        if (get(p) == eq)
        //            ans++;
        //    return ans;
        //}

        private class Cell
        {
            public int num; // either a num (1-8), blank(0), unclicked (-1),
                //or a mine (-2), or not-a-mine-but-number-unknown (-3)
            public Cell()
            {
                setNumber(-1);
            }
            public Cell(int n)
            {
                setNumber(n);
            }
            public void setNumber(int n)
            {
                num = n;
            }
            public void setMine()
            {
                num = -2;
            }
            public void setSafe()
            {
                num = -3;
            }
            public bool isNumber()
            {
                return num > 0;
            }
            public bool isMine()
            {
                return num == -2;
            } //guarenteed to be a mine
            public bool isUnsure()
            {
                return num == -1;
            }
            public bool notMine()
            {
                return !(isMine() || isUnsure());
            } //guarenteed to be not a mine
        }

        //Tests equality for two collections
        //source: http://dotnetperls.com/list-equals
        static bool unorderedEqual<T>(ICollection<T> a, ICollection<T> b)
        {
            // 1
            // Require that the counts are equal
            if (a.Count != b.Count)
            {
                return false;
            }
            // 2
            // Initialize new Dictionary of the type
            Dictionary<T, int> d = new Dictionary<T, int>();
            // 3
            // Add each key's frequency from collection A to the Dictionary
            foreach (T item in a)
            {
                int c;
                if (d.TryGetValue(item, out c))
                {
                    d[item] = c + 1;
                }
                else
                {
                    d.Add(item, 1);
                }
            }
            // 4
            // Add each key's frequency from collection B to the Dictionary
            // Return early if we detect a mismatch
            foreach (T item in b)
            {
                int c;
                if (d.TryGetValue(item, out c))
                {
                    if (c == 0)
                    {
                        return false;
                    }
                    else
                    {
                        d[item] = c - 1;
                    }
                }
                else
                {
                    // Not in dictionary
                    return false;
                }
            }
            // 5
            // Verify that all frequencies are zero
            foreach (int v in d.Values)
            {
                if (v != 0)
                {
                    return false;
                }
            }
            // 6
            // We know the collections are equal
            return true;
        }

        private void addGroup(Group groupToAdd) //don't add duplicates
        {
            if (!groups.Contains(groupToAdd))
                groups.Add(groupToAdd);
        }
        private void addNewGroup(Group newGroup) //checks for hints
        {
            //Check if the new hint can provide more information
            List<Group> tempToAdd = new List<Group>();
            foreach (Group g in groups)
            {
                Group tempGroup = g.inputGroup(newGroup);
                if (tempGroup != null)
                    tempToAdd.Add(tempGroup);
                tempGroup = newGroup.inputGroup(g);
                if (tempGroup != null)
                    tempToAdd.Add(tempGroup);
            }
            foreach (Group g in tempToAdd)
                addGroup(g);
            addGroup(newGroup);
        }
        private class Group : IEquatable<Group>
        {
            private List<Point> points = new List<Point>();
            private int mines;
            public Group(List<Point> ps, int m)
            {
                points = ps;
                mines = m;
            }
            //When p is learned to be a mine or not a mine
            //Returns true if changed something
            public bool inputCellAsMine(Point p, bool mine)
            {
                //process if p was a part of the group
                if (points.Remove(p))
                {
                    if (mine)
                        --mines;
                    return true;
                }
                return false;
            }
            //Will return a new Group that accounts for any information gleaned
            //from the sub-group, or null for no info
            public Group inputGroup(Group subgroup)
            {
                int mineDifference = mines - subgroup.mines;
                if (mineDifference < 0)
                    return null; //subgroup needs to be part of the main group

                //check if subgroup is a sub-part of this Group
                bool contained = true;
                foreach (Point p in subgroup.points)
                    if (!points.Contains(p))
                        contained = false;

                //If points A, B and C have 2 mines, but A and B have 1, then a
                //new group of C with 1 mine will be created
                if (contained)
                {
                    if (points.Count == subgroup.points.Count)
                        return null; //ignore duplicate groups

                    //make a new group
                    List<Point> remaining = new List<Point>();
                    foreach (Point p in points)
                        if (!subgroup.points.Contains(p))
                            remaining.Add(p);
                    return new Group(remaining, mines - subgroup.mines);
                }

                //Note every point in this Group not in the subgroup
                List<Point> extraPoints = new List<Point>();
                foreach (Point p in points)
                    if (!subgroup.points.Contains(p))
                        extraPoints.Add(p);

                //If A, B and C have 2 mines, and B, C, D, E have 4 mines, then
                //D and E are both mines. Declared by returning a D-and-E group
                if (extraPoints.Count == mineDifference)
                    return new Group(extraPoints, mineDifference);

                return null;
            }

            //returns true if sure about spaces being mines or not mines
            public bool mineInfo(ref List<Point> ps, ref bool areMines)
            {
                if (isEmpty())
                    return false; //nothing to say

                if (mines == points.Count) //all spaces are mines
                {
                    ps = points;
                    areMines = true;
                    return true;
                }
                if (mines == 0) //no spaces are mines
                {
                    ps = points;
                    areMines = false;
                    return true;
                }
                return false; //unsure about spaces
            }
            //once the Group has had all its points decided, it is now done
            public bool isEmpty()
            {
                return points.Count == 0;
            }
            public override bool Equals(object obj)
            {
                return this.Equals(obj as Group);
            }
            public bool Equals(Group g)
            {
                if (g == null)
                    return false;
                return unorderedEqual(points, g.points) && mines == g.mines;
            }
            public static bool operator ==(Group lhs, Group rhs)
            {
                // Check for null on left side.
                if (Object.ReferenceEquals(lhs, null))
                {
                    if (Object.ReferenceEquals(rhs, null))
                    {
                        // null == null = true.
                        return true;
                    }

                    // Only the left side is null.
                    return false;
                }
                // Equals handles case of null on right side.
                return lhs.Equals(rhs);
            }
            public static bool operator !=(Group lhs, Group rhs)
            {
                return !(lhs == rhs);
            }
            public override int GetHashCode()
            {
                //Groups SHOULDN'T be hashed because they are mutable
                return 0;
            }
        }
    }
}
