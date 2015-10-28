// Minesweeper Helper, by Carl Walsh May 2010
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Resources;


namespace Minesweeper_Helper
{
    
    class Program
    {
        //The main gives the user instructions, and acts as mediator between 
        //the logic, which decides which spaces are safe to click, and the 
        //system in/out, which handles reading info from screenshots and 
        //clicking on cells

        static void Main(string[] args)
        {
            bool GO_SLOW = true;
            bool DISTINGUISH_378 = true;
            bool PRINT_MINES = false;
            bool USE_SIMPLE = false;
            bool USE_COMPLEX = true; //TODO fix up booleans
            bool USE_PROB = true;
            Console.WriteLine("        [Note: leave any question blank for " +
                "default answers]\n\n" +
                              "Do you want to run in expedited mode? (y/n)?" +
                "  (for experienced users)");

            String reply = Console.ReadLine();
            if (reply.StartsWith("y"))
                GO_SLOW = false;

            reply = "";
            if (GO_SLOW)
            {
                Console.WriteLine("Navigate to an new open minesweeper " +
                    "window in at most 3 seconds (after ENTER)!\n" +
                                  "Have the window selected and entirely " +
                    "contained by the screen\n" +
                                  " (Make sure the mouse is hovering over " +
                    "any cell but the top left--could crash)\n\n" +
                                  "Distinguish between 3/7/8? (y/n)");
                reply = Console.ReadLine();
                if(reply.StartsWith("n"))
                    DISTINGUISH_378 = false;

                Console.WriteLine("Print mines on each iteration? (y/n)");
                reply = Console.ReadLine();
                if(!reply.StartsWith("n"))
                    PRINT_MINES = true;

                Console.WriteLine("There are different methods of solving a " +
                    "puzzle");
                Console.WriteLine("Use fast, simple one-step logic? (y/n)");
                reply = Console.ReadLine();
                if (reply.StartsWith("y"))
                    USE_SIMPLE = true;

                Console.WriteLine("Use slower, complex two-step logic? (y/n)");
                reply = Console.ReadLine();
                if (!reply.StartsWith("y"))
                    USE_COMPLEX = false;

                Console.WriteLine("Use guessing with probability? (y/n)");
                reply = Console.ReadLine();
                if (reply.StartsWith("n"))
                    USE_PROB = false;
            }

            //     *** Begin The Game***
            Console.WriteLine("\nGame Starting In:");
            for (int i = 3; i != -1; i--)
            {
                Console.WriteLine(i);
                Thread.Sleep(1000);
            }

            //If you are running into crashes right here, make sure the
            //mouse is positioned correctly and you're using Windows 
            //Minesweeper for Windows 7, with a reasonably small window

            //The minesweeper should be up at this point, so build the io
            SystemIO io;
            try
            {
                io = new SystemIO();
            }
            catch (Exception e)
            {
                Console.WriteLine("Build Failed! The cursors must not have been on the cells!");
                Console.WriteLine(e.Message);
                return;
            }
            io.setDistinguish378(DISTINGUISH_378);
            //io.debugger(); //TODO eventually remove

            int w = io.getWidth(),
                h = io.getHeight(),
                m = io.getTotalMines();
            if (GO_SLOW)
                Console.WriteLine("Game board read to be {0}x{1}, with {2} " +
                                  "mines!", w, h, m);

            //Now build the logic
            Logic logic = new Logic(w, h, USE_SIMPLE, USE_COMPLEX);
            //if (GO_SLOW)
            //{
                Console.Write("Using ");
                if (USE_SIMPLE)
                    if (USE_COMPLEX)
                        Console.Write("simple and complex ");
                    else
                        Console.Write("simple ");
                else
                    if (USE_COMPLEX)
                        Console.Write("complex ");
                    else
                    {
                        Console.Write("no ");
                        if (!USE_PROB)
                            Console.Write("(yes, really nothing!) ");
                    }
                Console.Write("logic");
                if(USE_PROB)
                    Console.Write(" (and probability as backup)");
                Console.WriteLine(".");
            //}

            Point mid = new Point(w / 2, h / 2); //click the middle to start
            List<Point> toClick = new List<Point>();
            toClick.Add(mid);
            int roundNumber = 0, loopNumber = 0;
            while (true) //loops controls the action
            {
                loopNumber++;
                if (toClick.Count == 0)
                {
                    KeyValuePair<List<Point>, List<Point>> probMoves = new KeyValuePair<List<Point>, List<Point>>();
                    if (USE_PROB)
                    {
                        probMoves = new Probability(w, h, m, io.getBoard()).getNextMoves();
                        io.inputMines(probMoves.Value);
                        toClick = probMoves.Key;
                        roundNumber++;
                        if (roundNumber > 50)
                        {
                            Console.WriteLine("This is a bad thing!!!");
                            Thread.Sleep(10000); //TODO remove
                        }
                    }
                    if (toClick.Count == 0 && probMoves.Value.Count == 0)
                    {
                        Thread.Sleep(2000);
                        Console.Write("...\n");
                    }
                }
                foreach (Point P in toClick)
                    io.click(P);
                Thread.Sleep(8);
                if (loopNumber == 1)
                    Thread.Sleep(100); // Sleep more on board generation!
                io.update(); //causes io to grab new screenshot

                if (io.getGameFinished())
                {
                    Console.WriteLine("Game Over! Play again?");
                    reply = Console.ReadLine();
                    if (reply.StartsWith("y"))
                        Main(args);
                    break;
                }

                if(PRINT_MINES)
                    io.printBoard();


                logic.setNums(io.getNums());
                if(GO_SLOW && PRINT_MINES)
                    logic.printLogic(); //TODO remove?

                io.select();
                KeyValuePair<List<Point>, List<Point>> moves=
                    logic.nextMoves();
                if (GO_SLOW)
                {
                    Console.WriteLine("Logic communicates:");
                    Console.Write("Spaces safe to click are: ");
                    foreach (Point p in moves.Key)
                        Console.Write("({0}, {1}) ", p.X, p.Y);
                    Console.WriteLine();
                    Console.Write("Spaces that are mines are: ");
                    foreach (Point p in moves.Value)
                        Console.Write("({0}, {1}) ", p.X, p.Y);
                    Console.WriteLine();
                }
                toClick = moves.Key;
                io.inputMines(moves.Value);

                //if (GO_SLOW)
                //{
                //    Console.ReadLine();
                    io.select();
                //}
            }
            
            Console.ReadLine();
            
        }
    }
}
