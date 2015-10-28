// Minesweeper Helper, by Carl Walsh August 2010
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Minesweeper_Helper
{
    /* Probability is used to suggest a next move in the game Mineseeper. A game
     * board is inputed, and all possible permutations of mines on the board are
     * computed. (Cases where many cells aren't next to hints are generalized.)
     * Based on what is the most likely, getNextMoves returns what the player
     * should do next.
     * http://nothings.org/games/minesweeper/ discusses this in-depth
     */
    class Probability
    {
        int WIDTH;
        int HEIGHT;
        int MINES;
        int[,] board; //0-8 are hints, -2 is mine, and -1 is an unknown cell

        bool[,] completelyRandom; //unknown which no hints influence

        List<bool?[,]> choices; //all possible combination of spaces
            //but only the spaces noted in influencedRandom are actually marked

        //b should have x and y as its dimensions
        public Probability(int x, int y, int m, int[,] b)
        {
            WIDTH = x;
            HEIGHT = y;
            MINES = m;
            board = b;
            completelyRandom = new bool[x, y];

            choices = new List<bool?[,]>();
            //Populated the random spaces that are influenced by hints
            bool?[,] guessBoard = new bool?[x, y];
            //Start the completelyRandom board
            for (int X = 0; X < x; ++X)
                for (int Y = 0; Y < y; ++Y)
                    completelyRandom[X, Y] = (board[X, Y] == -1);
            //Make the board false
            for (int X = 0; X < x; ++X)
                for (int Y = 0; Y < y; ++Y)
                    guessBoard[X, Y] = false;
            //Set every unknown space next to a hint as influenced unknown
            for (int X = 0; X < x; ++X)
                for (int Y = 0; Y < y; ++Y)
                    if (board[X, Y] >= 0) //it's a hint
                        foreach (Point p in nextTo(new Point(X, Y)))
                            if (board[p.X, p.Y] == -1) //and it's unknown
                            {
                                guessBoard[p.X, p.Y] = null;
                                completelyRandom[p.X, p.Y] = false;
                            }
            //Set all the mines to true
            for (int X = 0; X < x; ++X)
                for (int Y = 0; Y < y; ++Y)
                    if (board[X, Y] == -2)
                        guessBoard[X, Y] = true;

            determineMines(WIDTH - 1, -1, guessBoard); //populated choices
        }
        
        //helper functions
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
            return (0 <= p.X && p.X < WIDTH) &&
                   (0 <= p.Y && p.Y < HEIGHT);
        }

       /*The number of mines remaining will determine the combination logic
        * that weights the relitive likelyness of each layout occuring, and 
        * which points are least/most likely to contain mines, based on a 
        * completely random start.
        */
        //Returns safe move(s) / mine(s) as a pair
        public KeyValuePair<List<Point>, List<Point>> getNextMoves()
        {
            //Some of these numbers might be ridiculously big (10^180 or so for
            //all the choices in 600 cells with 200 mines, so use double with
            //its max of 10^308. The loss of precision is not a problem.
            int completelyRandomNumber = 0; //number of unknown cells without hints
            for(int y = 0; y < HEIGHT; ++y)
                for(int x = 0; x < WIDTH; ++x)
                    if(completelyRandom[x,y])
                        ++completelyRandomNumber;

            double totalPossibilities = 0; //total number of permutations
            //minePossibilities will be 0.0 to 1.0 chance of a mine being there
            double[,] minePossibilities = new double[WIDTH,HEIGHT];
            foreach (bool?[,] gridPossibile in choices)
            {
                int hintedMines = 0; //mines in with-hint unknown that are mines
                for (int y = 0; y < HEIGHT; ++y)
                    for (int x = 0; x < WIDTH; ++x)
                        if ((bool)gridPossibile[x, y])
                            ++hintedMines;
                
                //How many permutations are possible from the no-hints spaces
                double chanceFactor =
                    choose(completelyRandomNumber, MINES - hintedMines);

                //The chanceFactor is the number of permutations of these mines
                totalPossibilities += chanceFactor;
                for (int y = 0; y < HEIGHT; ++y)
                    for (int x = 0; x < WIDTH; ++x)
                        if ((bool)gridPossibile[x, y])
                            minePossibilities[x, y] += chanceFactor;

                //No-hints have a uniform chance of being a mine
                double noHintFactor = (chanceFactor * (MINES - hintedMines))
                    / completelyRandomNumber; //times factor b/c divided later
                for (int y = 0; y < HEIGHT; ++y)
                    for (int x = 0; x < WIDTH; ++x)
                        if (completelyRandom[x, y])
                            minePossibilities[x, y] += noHintFactor;
            }
            //Change probability to 0.0 to 1.0 values
            for (int y = 0; y < HEIGHT; ++y)
                for (int x = 0; x < WIDTH; ++x)
                    minePossibilities[x,y] /= totalPossibilities;


            //Find any "sure" values (and expect roundoff errors)
            List<Point> newSafes = new List<Point>();
            List<Point> newMines = new List<Point>();
            for (int y = 0; y < HEIGHT; ++y)
                for (int x = 0; x < WIDTH; ++x)
                    if (board[x, y] == -1 && 
                        Math.Abs(minePossibilities[x, y] - 0.5) > 0.4999999)
                        if (minePossibilities[x, y] < 0.5)
                            newSafes.Add(new Point(x, y));
                        else
                            newMines.Add(new Point(x, y));

            if (newSafes.Count + newMines.Count > 0)
                return new KeyValuePair<List<Point>, List<Point>>
                    (newSafes, newMines);

            //We didn't have any safe values, so make a best guess
            double bestChance = -1.0; //represents how sure, from 0.0 to 0.5
            Point bestGuess = new Point();
            for (int y = 0; y < HEIGHT; ++y)
                for (int x = 0; x < WIDTH; ++x)
                    if (board[x, y] == -1 &&
                        Math.Abs(minePossibilities[x, y] - 0.5) > bestChance)
                    {
                        bestChance = Math.Abs(minePossibilities[x, y] - 0.5);
                        bestGuess = new Point(x, y);
                    }
            //slighly favor clicking over mines, as the number is more helpful
            if (minePossibilities[bestGuess.X, bestGuess.Y] < 0.50001)
                newSafes.Add(bestGuess);
            else
                newMines.Add(bestGuess);

            if (bestChance != -1.0)
            {
                Console.WriteLine("This has {2}% chance of success: I guess that !={0} is {1}a mine",
                                  bestGuess.ToString(), (newSafes.Count == 1 ? "not " : ""), (int)((bestChance + 0.5) * 100));
                for (int y = Math.Max(0, bestGuess.Y - 2); y < Math.Min(HEIGHT, bestGuess.Y + 3); ++y)
                {
                    for (int x = Math.Max(0, bestGuess.X - 2); x < Math.Min(WIDTH, bestGuess.X + 3); ++x)
                        if (x == bestGuess.X && y == bestGuess.Y)
                            Console.Write("! ");
                        else if (board[x, y] == -1)
                            Console.Write("? ");
                        else if (board[x, y] == -2)
                            Console.Write("* ");
                        else if (board[x, y] == 0)
                            Console.Write("  ");
                        else
                            Console.Write(board[x, y] + " ");
                    Console.WriteLine();
                }
            }
            return new KeyValuePair<List<Point>, List<Point>>
                (newSafes, newMines);
        }
        private double choose(int n, int k)
        {
            if (n < k || k < 0 || n < 0) //there may not be more mines than 
                return 0.0;              //spaces, or "negative" mines

            double result = 1D;
            for (int i = 2; i <= Math.Min(k, n - k); ++i)
                result /= i; //divide THEN multiply to increase range
            for (int i = Math.Max(k, n - k) + 1; i <= n; ++i)
                result *= i;

            return result;
        } //Probability function

       /*  Populates the global List<bool?[,]>choices with every possible game
        * board.
        *    The recursive function will work as follows: Each call will have a
        * valid grid being passed to the next level, as well as the last point
        * it tried true and false on. It will try true on the next and if valid
        * call the next level, updating the point to the next location to 
        * check. Same for false. The checking will only check around the 
        * changed square, since the rest of the grid is assumed correct.
        *    The working grid will be reprsented by the bool? type, where null
        * means the cell is unknown, true means there is a mine there and false
        * is no mine.
        *    Once each "influenced point" (an unknown next to a hint) has been
        * named true or false, then grid is saved to choices.
        */
        private void determineMines(int lastX, int lastY, bool?[,] mines)
        {
            //Find the current location of an unknown
            int x = lastX;
            int y = lastY;
            do
            {
                if (++x == WIDTH)
                {
                    x = 0;
                    ++y;
                }
                if (y == HEIGHT) //because all points are known, we are done!
                {
                    choices.Add(mines);
                    return;
                }
            } while (mines[x, y].HasValue);

            //Test the true and false cases
            mines[x, y] = true;
            if (correctBoard(x, y, mines))
                determineMines(x, y, (bool?[,])mines.Clone());
            mines[x, y] = false; //(why go to the effort of making it generic?)
            if (correctBoard(x, y, mines))
                determineMines(x, y, (bool?[,])mines.Clone());
        }
        //returns if the board is consistent
        private bool correctBoard(int x, int y, bool?[,] newBoard)
        {
            foreach (Point p in nextTo(new Point(x,y)))
                if (!correctHint(p, newBoard))
                    return false;
            return true;
        }
        private bool correctHint(Point hint, bool?[,] newBoard)
        {
            if (board[hint.X, hint.Y] < 0)
                return true; //Nothing is wrong if the cell is not a hint

            int minMines = 0; //there must be this many mines
            int maxMines = 0; //but no more than this many
            foreach (Point p in nextTo(hint))
                if (!newBoard[p.X, p.Y].HasValue)
                    ++maxMines; //null values potentially are a mine
                else if (newBoard[p.X, p.Y].Value)
                {
                    ++minMines;
                    ++maxMines;
                }

            return (board[hint.X, hint.Y] >= minMines &&
                    board[hint.X, hint.Y] <= maxMines);
        }

        //debug functions
        private void printBoard(bool?[,] toPrint)
        {
            for (int y = 0; y < HEIGHT; ++y)
            {
                for (int x = 0; x < WIDTH; ++x)
                    if (board[x, y] == -1)
                    {
                        if (completelyRandom[x, y])
                            Console.Write("? ");
                        else
                            if (toPrint[x, y].HasValue)
                                if ((bool)toPrint[x, y])
                                    Console.Write("* ");
                                else
                                    Console.Write("- ");
                    }
                    else if (board[x, y] == -2)
                        Console.Write("  ");
                    else
                        Console.Write(board[x, y] + " ");
                Console.WriteLine();
            }
        }
    }
}
