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
        int width, height;
        int[,] board; //reprents squares in (x,y) format, as a num 0-8, while
                      //-1 represents an unclicked state, and -2 is a mine
        public Logic(int w, int h)
        {
            width = w;
            height = h;
            board = new int[w, h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    board[x, y] = -1;
        }
        private List<Point> newMines;
        public List<Point> nextMoves() //returns newly decided safe spaces
        {
            List<Point> ans = new List<Point>();
            newMines = new List<Point>();
            bool madeChange = true;
            while (madeChange)
            {
                madeChange = false;
                Point p = new Point(0,0);
                for (; p.Y < height; p.Y++)
                    for (p.X = 0; p.X < width; p.X++)
                        if (isNumber(p))
                        {
                            List<Point> cells = nextTo(p);
                            if (get(p) == mines(cells))
                            { //no more mines around this
                                foreach (Point c in cells)
                                    if (unsure(c))
                                    {
                                        set(c, -3);
                                        ans.Add(new Point(c.X, c.Y));
                                        madeChange = true;
                                    }
                            }
                            else if (get(p) == mines(cells) + unsures(cells))
                            { //only mines around this
                                foreach(Point c in cells)
                                    if(unsure(c))
                                    {
                                        set(c, -2);
                                        newMines.Add(c);
                                        madeChange = true;
                                    }
                            }
                        }
            }
            return ans;
        }
        public List<Point> getNewMines() //returns newly decided mines
        {
            return newMines;
        } 
        public void setNum(Point p, int num)
        {
            //if (num >= 0 && num < 9)
            set(p, num);
        }
        public void setNums(List<KeyValuePair<Point, int>> vals)
        {
            foreach (KeyValuePair<Point, int> x in vals)
                setNum(x.Key, x.Value);
        }


         //TODO: make the number updates handled in SystemIO, so I don't
         //parse the same numbers more than once.
         

        private int get(Point p)
        {
            return board[p.X, p.Y];
        }
        private void set(Point p, int num)
        {
            board[p.X, p.Y] = num;
        }
        private int nextToNum(Point p)//how many cells around a cell
        {
            int numEdges = 0;
            if (p.X == 0 || p.X + 1 == width)
                numEdges++;
            if (p.Y == 0 || p.Y + 1 == height)
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
        private bool valid(Point p)
        {
            return (0 <= p.X && p.X < width) &&
                   (0 <= p.Y && p.Y < height);
        }
        private bool isNumber(Point p)
        {
            return get(p) > 0;
        }
        private bool mine(Point p)
        {
            return get(p) == -2;
        }
        private int mines(List<Point> l)
        {
            return areEqual(l, -2);
        }
        private bool notMine(Point p)
        {
            return !(mine(p) || unsure(p));
        }
        private int notMines(List<Point> l)
        {
            return l.Count - mines(l) - unsures(l);
        }
        private bool unsure(Point p)
        {
            return get(p) == -1;
        }
        private int unsures(List<Point> l)
        {
            return areEqual(l, -1);
        }
        private int areEqual(List<Point> l, int eq)
        {
            int ans = 0;
            foreach (Point p in l)
                if (get(p) == eq)
                    ans++;
            return ans;
        }

        private class Cell
        {
            int num; // either a num (1-8), or blank(0),
                     // or unclicked (-1)or a mine (-2)
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
            public bool isNumber()
            {
                return num > 0;
            }
            public bool isMine()
            {
                return num == -2;
            }
            public bool isUnsure()
            {
                return num == -1;
            }
            public bool notMine()
            {
                return !(isMine() || isUnsure());
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
        private class Collection
        {
            List<Group> groups; //all the hint information
            List<KeyValuePair<Point, bool>> toReturn;//TODO
            bool[,] known; //known spaces aren't part of hints
            int width;
            int height;
            public Collection(int x, int y)
            {
                width = x;
                height = y;
                known = new bool[x, y];
                groups = new List<Group>();
            }
            public void addMines(Point p, int mines)
            {

            }
            private bool valid(Point p)
            {
                return (0 <= p.X && p.X < width) &&
                       (0 <= p.Y && p.Y < height) &&
                       !known[p.X, p.Y];
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

            private class Group
            {
                private List<Point> points = new List<Point>();
                private int mines;
                public Group(List<Point> ps, int m)
                {
                    points = ps;
                    mines = m;
                }
                //When p is learned to be a mine or not a mine--true means mine
                public void inputCellAsMine(Point p, bool b)
                {
                    //if the point was part of the group, and a mine
                    if (points.Remove(p) && b) 
                        --mines;
                }
                //The strong part of Group--if another Group is entirely 
                //contained by this Group, then this Group will create a new 
                //Group that is reduced to the remaining cells and mines.
                //For example, if points A, B and C have 2 mines, but A and B
                //have 1, then a new group of C with 1 mine will be created
                public Group inputGroup(Group subgroup)
                {
                    //check if subgroup is a sub-part of this Group
                    bool contained = true;
                    foreach (Point p in subgroup.points)
                        if (!points.Contains(p))
                            contained = false;

                    if (contained)
                    {
                        //make a new group
                        List<Point> remaining = new List<Point>();
                        foreach (Point p in points)
                            if (!subgroup.points.Contains(p))
                                remaining.Add(p);
                        return new Group(remaining, mines - subgroup.mines);
                    }
                    return null;
                }
                //returns true if sure about spaces being mines or not mines
                public bool mineInfo(ref List<Point> ps, ref bool areMines)
                {
                    if (points.Count == mines) //all spaces are mines
                    {
                        ps = points;
                        areMines = true;
                        return true;
                    }
                    if (points.Count == 0) //no spaces are mines
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
            }
        }
    }
}
