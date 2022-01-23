using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Wordle
{
    const int wordLen = 5;
    const int maxGuesses = 6;
    const string dictionaryFile = @"words.txt";
    const string defaultGuess = @"tramp";        // empirically, this word works best

    static void Score( string solution, string guess, char [] score, bool [] slotsUsed )
    {
        for ( int i = 0; i < wordLen; i++ )
        {
            if ( guess[ i ] == solution[ i ] )
            {
                score[ i ] = 'g';
                slotsUsed[ i ] = true;
            }
            else
            {
                score[ i ] = ' ';
                slotsUsed[ i ] = false;
            }
        }

        for ( int i = 0; i < wordLen; i++ )
        {
            if ( 'g' != score[ i ] )
            {
                for ( int j = 0; j < wordLen; j++ )
                {
                    if ( i != j && ! slotsUsed[ j ] && guess[ i ] == solution[ j ] )
                    {
                        slotsUsed[ j ] = true;
                        score[ i ] = 'y';
                    }
                }
            }
        }
    } //Score

    static bool SameScore( char [] chars, string str )
    {
        for ( int i = 0; i < wordLen; i++ )
            if ( chars[ i ] != str[ i ] )
                return false;
        return true;
    } //SameScore

    static bool IsScoreValid( string score )
    {
        if ( score.Length != wordLen )
            return false;

        foreach ( char c in score )
            if ( 'g' != c && 'y' != c && ' ' != c )
                return false;

        return true;
    } //IsScoreValid

    static string FindNextAttempt( ref int nextGuess, string [] guesses, string [] scores, char [] score,
                                   bool [] slotsUsed, List<string> dictionary, int currentGuess, int startingGuess )
    {
        string attempt = null;

        do
        {
            attempt = dictionary[ nextGuess++ ];
            if ( nextGuess >= dictionary.Count() )
                nextGuess = 0;

            bool newGuessMatchesPriorGuesses = true;
    
            for ( int g = 0; g < currentGuess; g++ )
            {
                Score( attempt, guesses[ g ], score, slotsUsed );
                if ( !SameScore( score, scores[ g ] ) )
                {
                    newGuessMatchesPriorGuesses = false;
                    break;
                }
            }
    
            if ( newGuessMatchesPriorGuesses )
                break;

            if ( nextGuess == startingGuess )
            {
                Console.WriteLine( "Error: the solution word isn't in the dictionary file {0}", dictionaryFile );
                Environment.Exit( 1 );
            }
        } while( true );

        return attempt;
    } //FindNextAttempt

    static void Usage( string error )
    {
        Console.WriteLine( "error: {0} ", error );
        Console.WriteLine( "Usage: wordle [-a] [-g:guess] [-i] [-o] [-r] [-s:solution] [-v]" );
        Console.WriteLine( "  -a          Test against actual wordle solutions, not the whole dictionary" );
        Console.WriteLine( "  -g:guess    The first guess word to use. Default is \"{0}\"", defaultGuess );
        Console.WriteLine( "  -i          Interactive mode. Use this to have the app play wordle for you." );
        Console.WriteLine( "  -m:X        Limit guesses to just X (2-12). Default is {0}", maxGuesses );
        Console.WriteLine( "  -o          Use just one core" );
        Console.WriteLine( "  -p          Play wordle" );
        Console.WriteLine( "  -r          Don't Randomize the order of words in the dictionary" );
        Console.WriteLine( "  -s:solution The word to search for instead of the whole dictionary" );
        Console.WriteLine( "  -v          Verbose logging of failures to find a solution. -V for successes too" );
        Console.WriteLine( "  notes:      Assumes {0} in the current folder contains a dictionary", dictionaryFile );
        Console.WriteLine( "              Only one of -a or -s can be specified" );
        Console.WriteLine( "  samples:    wordle              solve each word in the dictionary" );
        Console.WriteLine( "              wordle -i           interactive mode for use with the wordle app" );
        Console.WriteLine( "              wordle -i -r        interactive mode, but don't randomize dictionary word order" );
        Console.WriteLine( "              wordle -i /g:group  interactive mode, but make the first guess \"group\"" );
        Console.WriteLine( "              wordle /s:tangy /V  solve for tangy and show the guesses" );
        Console.WriteLine( "              wordle -a -v        solve for known wordle solutions and show failures." );
        Console.WriteLine( "              wordle -a -V        solve for known wordle solutions and show details." );
        Environment.Exit( 1 );
    } //Usage

    static void Main( string[] args )
    {
        bool testActual = false;
        bool verbose = false;
        bool verboseSuccess = false;
        bool oneCore = false;
        bool randomizeDictionary = true;
        bool interactiveMode = false;
        bool playWordleMode = false;
        string firstGuess = defaultGuess;
        string userSolution = null;
        int maxAllowedGuesses = maxGuesses;
        string allgreen = new string( 'g', wordLen );

        for ( int i = 0; i < args.Length; i++ )
        {
            if ( '-' == args[i][0] || '/' == args[i][0] )
            {
                string argUpper = args[i].ToUpper();
                string arg = args[i];
                char c = argUpper[1];

                if ( 'A' == c )
                    testActual = true;
                else if ( 'G' == c )
                {
                    if ( arg.Length != ( 3 + wordLen ) )
                        Usage( "-f parameter has invalid value" );

                    firstGuess = arg.Substring( 3 ).ToLower();
                }
                else if ( 'I' == c )
                    interactiveMode = true;
                else if ( 'M' == c )
                {
                    if ( arg.Length > 5 || arg.Length < 4 )
                        Usage( "invalid /m argument" );

                    maxAllowedGuesses = int.Parse( arg.Substring( 3 ) );
                    if ( maxAllowedGuesses < 2 || maxAllowedGuesses > 12 )
                        Usage( "argument for /m is out of range" );
                }
                else if ( 'O' == c )
                    oneCore = true;
                else if ( 'P' == c )
                    playWordleMode = true;
                else if ( 'R' == c )
                    randomizeDictionary = false;
                else if ( 'S' == c )
                {
                    if ( arg.Length != ( 3 + wordLen ) )
                        Usage( "-s parameter has invalid value" );

                    userSolution = arg.Substring( 3 ).ToLower();
                }
                else if ( 'V' == c )
                {
                    verbose = true;
                    verboseSuccess = 'V' == args[i][1];
                }
                else
                    Usage( "invalid argument: " + args[i] );
            }
            else
                Usage( "no argument flag - or / specified" );
        }

        if ( testActual && ( null != userSolution ) )
            Usage( " -a and -s are mutually exclusive" );

        if ( playWordleMode && interactiveMode )
            Usage( " -p and -i are mutually exclusive" );

        List<string> dictionary = new List<string>();
        foreach ( string line in System.IO.File.ReadLines( dictionaryFile ) )
            if ( wordLen == line.Length  &&
                 -1 == line.IndexOf( '.' ) &&
                 -1 == line.IndexOf( '-' ) &&
                 -1 == line.IndexOf( '\'' ) )
               dictionary.Add( line.ToLower() );

        if ( randomizeDictionary )
        {
            Random rand = new Random( Environment.TickCount );
            for ( int r = 0; r < dictionary.Count(); r++ )
            {
                int x = rand.Next( dictionary.Count() );
                int y = rand.Next( dictionary.Count() );
                string t = dictionary[ x ];
                dictionary[ x ] = dictionary[ y ];
                dictionary[ y ] = t;
            }
        }

        if ( playWordleMode )
        {
            Random rand = new Random( Environment.TickCount );
            string solution = dictionary[ rand.Next( dictionary.Count() ) ];
            char [] score = new char[ wordLen ];
            bool [] slotsUsed = new bool[ wordLen ];

            for ( int attempt = 0; attempt < maxAllowedGuesses; attempt++ )
            {
                string userGuess = null;

                do
                {
                    Console.Write( "enter guess: " );
                    userGuess = Console.ReadLine().ToLower();

                    if ( wordLen != userGuess.Length )
                    {
                        Console.WriteLine( "Guesses must be {0} letters long; try again", wordLen );
                        continue;
                    }

                    if ( -1 == dictionary.IndexOf( userGuess ) )
                    {
                        Console.WriteLine( "Guess isn't in the dictionary; try again" );
                        continue;
                    }

                    break;
                } while ( true );

                Score( solution, userGuess, score, slotsUsed );
                string strScore = new string( score );
                Console.WriteLine( "score: '" + strScore + "'" );

                if ( 0 == String.Compare( allgreen, strScore ) )
                {
                    Console.WriteLine( allgreen + "\nYou found the solution!" );
                    Environment.Exit( 0 );
                }
            }

            Console.WriteLine( "You didn't find the solution, which was {0}", solution );
            Environment.Exit( 1 );
        }

        int startingGuess = dictionary.IndexOf( firstGuess );
        if ( -1 == startingGuess )
            Usage( "Can't find specified first word in the dictionary: " + firstGuess );

        if ( interactiveMode )
        {
            Console.WriteLine( "guess 0: " + firstGuess );
            int currentGuess = 0;
            string [] scores = new string[ maxAllowedGuesses ];
            string [] guesses = new string[ maxAllowedGuesses ];
            guesses[ 0 ] = firstGuess;
            int nextGuess = startingGuess;
            char [] score = new char[ wordLen ];
            bool [] slotsUsed = new bool[ wordLen ];

            do
            {
                string scoreGiven;

                do
                {
                    Console.Write( "enter score: " );
                    scoreGiven = Console.ReadLine().ToLower();
                    if ( IsScoreValid( scoreGiven ) )
                        break;

                    Console.WriteLine( "score must contain {0} space, y, or g characters. for example: 'y g y'", wordLen );
                } while( true );

                scores[ currentGuess++ ] = scoreGiven;
                if ( 0 == String.Compare( scoreGiven, allgreen ) )
                {
                    Console.WriteLine( "success in {0} tries!", currentGuess );
                    Environment.Exit( 0 );
                }

                if ( currentGuess == maxAllowedGuesses )
                    break;

                string attempt = FindNextAttempt( ref nextGuess, guesses, scores, score, slotsUsed, dictionary, currentGuess, startingGuess );
                guesses[ currentGuess ] = attempt;
                Console.WriteLine( "guess {0}: {1}", currentGuess, attempt );
            } while ( true );

            Console.WriteLine( "failed to find a solution!" );
            Environment.Exit( 0 );
        }

        string [] actualSolutions = // actual puzzle solutions
        {
            "tapir", "troll",
            "rebus", "boost", "truss", "siege", "tiger", "banal", "slump", "crank", "gorge", "query",
            "drink", "favor", "abbey", "tangy", "panic", "solar", "shire", "proxy", "point", "robot",
            "prick", "wince", "crimp",
        };

        string [] userSolutions = { userSolution };
        IList<string> testCases = ( null != userSolution ) ? ( IList<string> ) userSolutions :
                                  testActual ? ( IList<string> ) actualSolutions : dictionary;
        int successes = 0;
        int failures = 0;
        int attemptTotal = 0;
        Stopwatch stopWatch = Stopwatch.StartNew();

        Parallel.For ( 0, testCases.Count(), new ParallelOptions{ MaxDegreeOfParallelism = oneCore ? 1 : -1 },
                       iSolution =>
        {
            string [] scores = new string[ maxAllowedGuesses ];
            string [] guesses = new string[ maxAllowedGuesses ];
            string solution = testCases[ iSolution ];
            bool success = false;
            int nextGuess = startingGuess;
            char [] score = new char[ wordLen ];
            bool [] slotsUsed = new bool[ wordLen ];

            for ( int currentGuess = 0; currentGuess < maxAllowedGuesses; currentGuess++ )
            {
                string attempt = FindNextAttempt( ref nextGuess, guesses, scores, score, slotsUsed, dictionary, currentGuess, startingGuess );
                Score( solution, attempt, score, slotsUsed );
                guesses[ currentGuess ] = attempt;
                scores[ currentGuess ] = new string( score );
                Interlocked.Increment( ref attemptTotal );

                if ( SameScore( score, allgreen ) )
                {
                    success = true;
                    break;
                }
            }

            if ( success )
                Interlocked.Increment( ref successes );
            else
                Interlocked.Increment( ref failures );

            if ( verboseSuccess || ( !success && verbose ) )
            {
                lock ( args )
                {
                    Console.WriteLine( "{0} for {1}", success ? "solved" : "could not solve", solution );
                    for ( int g = 0; g < maxAllowedGuesses && null != guesses[g]; g++ )
                        Console.WriteLine( "  attempt {0}: {1}, score '{2}'", g, guesses[ g ], scores[ g ] );
                }
            }
        } );

        Console.WriteLine( "total games {0}, successes {1}, failures {2}, average attempts {3:.00}, success rate {4:.00}%, {5} milliseconds",
                           testCases.Count(), successes, failures, (float) attemptTotal / (float) testCases.Count(),
                           100.0 * (float) successes / (float) testCases.Count(), stopWatch.ElapsedMilliseconds );
    } //Main
} //Wordle
