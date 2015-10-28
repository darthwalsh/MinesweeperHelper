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
            SystemIO io = new SystemIO();
            io.setDistinguish378(DISTINGUISH_378);
            //io.debugger(); //TODO eventually remove

            int w = io.getWidth(),
                h = io.getHeight(),
                m = io.getMinesRemaining();
            if (GO_SLOW)
                Console.WriteLine("Game board read to be {0}x{1}, with {2} " +
                                  "mines!", w, h, m);

            Logic logic = new Logic(w, h); //Now build the logic
            
            Point mid = new Point(w / 2, h / 2); //click the middle to start
            List<Point> toClick = new List<Point>();
            toClick.Add(mid);
            while (true) //loops controls the action
            {
                if (toClick.Count == 0)
                {
                    Thread.Sleep(500);
                    Console.Write("...\n");
                }
                foreach (Point P in toClick)
                    io.click(P);
                Thread.Sleep(8);
                io.update(); //causes io to grab new screenshot

                if(PRINT_MINES)
                    io.printBoard();

                logic.setNums(io.getNums());
                toClick = logic.nextMoves();
                io.inputMines(logic.getNewMines());

                if (GO_SLOW)
                {
                    Console.ReadLine();
                    io.select();
                }
            }
            
            Console.ReadLine();
            
        }
    }
}
