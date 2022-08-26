using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Wordle
{
    class WordCount
    {
        public string word;
        public int count;
    
        public WordCount( string w, int c )
        {
            word = w;
            count = c;
        }
    }
    
    public class WordCountComparer : IComparer
    {
        int IComparer.Compare( Object x, Object y )
        {
            WordCount wcx = (WordCount) x;
            WordCount wcy = (WordCount) y;
            return wcy.count - wcx.count;
        }
    }

    const int defaultWordLen = 5;
    static int wordLen = defaultWordLen;
    const int maxGuesses = 6;
    const string dictionaryFile = @"words.txt";

    static string [] defaultGuesses =
    {
        "",
        "a",
        "on",
        "pat",
        "tamp",
        "patch",
        "swampy",
        "rhombic",
        "doorbell",
        "pneumonia",
        "presenting",
        "abandonment",          // words of length 11 and longer will always win in 6.
        "abbreviation",
        "commercialism",
        "disillusioning",
        "trustworthiness",
    };

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
                        score[ i ] = 'y';
                        slotsUsed[ j ] = true;
                        break;
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

    static int FindNewLetters( string nextGuess, string priorScore, string priorGuess, char [] newLetters )
    {
        int len = 0;

        // Retain letters that aren't known correct -- 'g'

        for ( int n = 0; n < nextGuess.Length; n++ )
        {
            if ( 'g' != priorScore[ n ] )
                newLetters[ len++ ] = nextGuess[ n ];
        }

        // remove duplicate letters

        Array.Sort( newLetters, 0, len );

        int i = 0;
        while ( i < ( len - 1 ) )
        {
            if ( newLetters[ i ] == newLetters[ i + 1 ] )
            {
                for ( int j = i + 1; j < ( len - 1 ); j++ )
                    newLetters[ j ] = newLetters[ j + 1 ];
                len--;
            }
            else
                i++;
        }

        return len;
    } //FindNewLetters

    // This method results in about a 1% better game winning rate when solving for all possible words.
    // It's not optimized for performance or readability. It's still in experimental mode and will
    // be cleaned up when it's working better.
    // This code is heavily dependent on characters being in the ascii range of 'a'-'z'.

    static string FindNextAttempt2( ref int nextGuess, string [] guesses, string [] scores, char [] score,
                                    bool [] slotsUsed, List<string> dictionary, int currentGuess, int startingGuess,
                                    List<string> validGuesses, bool bestGuess )
    {
        if ( 0 == currentGuess )
            return dictionary[ nextGuess ];

        if ( 1 == currentGuess )
        {
            // find all valid guesses given the score of the opening word

            string firstGuess = guesses[ 0 ];
            string firstScore = scores[ 0 ];
    
            for ( int i = 0; i < dictionary.Count(); i++ )
            {
                string word = dictionary[ i ];
    
                if ( 0 != String.Compare( word, firstGuess ) )
                {
                    Score( word, firstGuess, score, slotsUsed );
                    if ( SameScore( score, firstScore ) )
                        validGuesses.Add( word );
                }
            }
        }
        else
        {
            // remove the words that no longer fit given the latest score.
            // This is a ripple copy in a while loop, so it could be a lot faster.

            int latestIndex = currentGuess - 1;
            int i = 0;

            while ( i < validGuesses.Count() )
            {
                string word = validGuesses[ i ];
                bool remove = ( 0 == String.Compare( word, guesses[ latestIndex ] ) );

                if ( !remove )
                {
                    Score( word, guesses[ latestIndex ], score, slotsUsed );
                    remove = !SameScore( score, scores[ latestIndex ] );
                }

                if ( remove )
                    validGuesses.RemoveAt( i );
                else
                    i++;
            }
        }

        if ( 0 == validGuesses.Count() )
        {
            Console.WriteLine( "Error: the solution word isn't in the dictionary file {0}", dictionaryFile );
            Environment.Exit( 1 );
        }

        // If there are 2 or fewer remaining guesses, just grab the first at random

        if ( validGuesses.Count() <= 2 )
            return validGuesses[ 0 ];

        // record how frequently each letter is found in the set of valid guesses

        int [] letterCounts = new int[ 26 ];
        int lastGuess = currentGuess - 1;
        int wordLength = validGuesses[ 0 ].Length;

        for ( int i = 0; i < validGuesses.Count(); i++ )
        {
            for ( int l = 0; l < wordLength; l++ )
            {
                if ( 'g' != scores[ lastGuess ][ l ] )
                    letterCounts[ validGuesses[ i ][ l ] - 'a' ]++;
            }
        }

        // Score each valid guess based on how well each letter divides the space of remaining words.
        // Letters found in half the words are best. Letters found once or in every word are worst.
        // Distance in n-dimensions is a sum of squares of distance in each dimension.
        // Don't bother finding the square root of the final distance, since it won't change anything.

        int halfWords = validGuesses.Count() / 2;
        char [] newLetters = new char[ wordLength ];
        int bestScore = bestGuess ? int.MaxValue : 0;
        int bestWord = 0;

        for ( int i = 0; i < validGuesses.Count(); i++ )
        {
            int charCount = FindNewLetters( validGuesses[ i ], scores[ lastGuess ], guesses[ lastGuess ], newLetters );
            int wordScore = 0;

            for ( int l = 0; l < charCount; l++ )
            {
                int distance = halfWords - letterCounts[ newLetters[ l ] - 'a' ];
                wordScore += ( distance * distance);
            }

            // Penalize non-unique letters. This also penalizes 'g' letters, but does so equally for all.

            for ( int l = charCount; l < wordLength; l++ )
                wordScore += ( halfWords * halfWords );

            // Find the word with the lowest (or highest for worst) score. Score is based on closeness to the center of all dimensions

            if ( bestGuess ? ( wordScore < bestScore ) : ( wordScore > bestScore ) )
            {
                bestWord = i;
                bestScore = wordScore;
            }
        }

        return validGuesses[ bestWord ];
    } //FindNextAttempt2

    static void Usage( string error )
    {
        Console.WriteLine( "error: {0} ", error );
        Console.WriteLine( "Usage: wordle [-a] [-g:guess] [-i] [-m:X] [-o] [-p] [-r] [-s:solution] [-v] [-x]" );
        Console.WriteLine( "  -a          Test against actual wordle solutions, not the whole dictionary" );
        Console.WriteLine( "  -d          Show the # of words in the dictionary matching the word length" );
        Console.WriteLine( "  -f          Try every word as a first guess to see what works best/worst in several iterations." );
        Console.WriteLine( "  -g:guess    The first guess word to use. Default is \"{0}\"", defaultGuesses[ defaultWordLen ] );
        Console.WriteLine( "  -i          Interactive mode. Use this to have the app play wordle for you." );
        Console.WriteLine( "  -l:X        Word Length. Default is {0}. Must be 1-15.", defaultWordLen );
        Console.WriteLine( "  -m:X        Limit guesses to just X (2-12). Default is {0}", maxGuesses );
        Console.WriteLine( "  -o          Use just one core" );
        Console.WriteLine( "  -p          Play wordle" );
        Console.WriteLine( "  -r          Don't Randomize the order of words in the dictionary" );
        Console.WriteLine( "  -s:solution The word to search for instead of the whole dictionary" );
        Console.WriteLine( "  -v          Verbose logging of failures to find a solution. -V for successes too" );
        Console.WriteLine( "  -x          Use experimental algorithm for finding the best guess. -X for worst guess" );
        Console.WriteLine( "  notes:      Assumes {0} in the current folder contains a dictionary", dictionaryFile );
        Console.WriteLine( "              Only one of -a or -s can be specified" );
        Console.WriteLine( "              Only one of -f, -i, or -p can be specified" );
        Console.WriteLine( "              -f allows the iteration count (default 4) to be specified with -f:X" );
        Console.WriteLine( "  samples:    wordle              solve each word in the dictionary" );
        Console.WriteLine( "              wordle -i           interactive mode for use with the wordle app" );
        Console.WriteLine( "              wordle -i -r        interactive mode, but don't randomize dictionary word order" );
        Console.WriteLine( "              wordle -i /g:group  interactive mode, but make the first guess \"group\"" );
        Console.WriteLine( "              wordle /s:tangy /V  solve for tangy and show the guesses" );
        Console.WriteLine( "              wordle -a -v        solve for known wordle solutions and show failures." );
        Console.WriteLine( "              wordle -a -V        solve for known wordle solutions and show details." );
        Environment.Exit( 1 );
    } //Usage

    static bool IsAllAlpha( string s )
    {
        foreach ( char c in s )
        {
            if ( c < 'a' || c > 'z' )
                return false;
        }

        return true;
    } //IsAllAlpha

    static void RandomizeList<T>( List<T> list )
    {
        Random rand = new Random( Environment.TickCount );
        int count = list.Count();

        for ( int r = 0; r < count; r++ )
        {
            int x = rand.Next( count );
            int y = rand.Next( count );
            T temp = list[ x ];
            list[ x ] = list[ y ];
            list[ y ] = temp;
        }
    } //RandomizeList<T>

    static void SolveForAllWords( IList<string> testCases, int startingGuess, List<string> dictionary,
                                  ref int successesResult, ref int failuresResult, ref int attemptsResult,
                                  bool experimentalAlgorithm, bool oneCore, int maxAllowedGuesses, bool bestGuess, string allgreen,
                                  bool verboseSuccess, bool verbose )
    {
        int successes = 0, failures = 0, attempts = 0;

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
            List<string> validGuesses = experimentalAlgorithm ? new List<string>() : null;
            int [] remainingGuesses = experimentalAlgorithm ? new int[ maxAllowedGuesses ] : null;

            for ( int currentGuess = 0; currentGuess < maxAllowedGuesses; currentGuess++ )
            {
                string attempt = experimentalAlgorithm ? FindNextAttempt2( ref nextGuess, guesses, scores, score, slotsUsed, dictionary,
                                                                           currentGuess, startingGuess, validGuesses, bestGuess ) :
                                                         FindNextAttempt( ref nextGuess, guesses, scores, score, slotsUsed, dictionary,
                                                                          currentGuess, startingGuess );
                Score( solution, attempt, score, slotsUsed );
                guesses[ currentGuess ] = attempt;
                scores[ currentGuess ] = new string( score );
                if ( experimentalAlgorithm )
                    remainingGuesses[ currentGuess ] = validGuesses.Count();
                Interlocked.Increment( ref attempts );

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
                lock ( testCases )
                {
                    Console.WriteLine( "{0} for {1}", success ? "solved" : "could not solve", solution );
                    for ( int g = 0; g < maxAllowedGuesses && null != guesses[g]; g++ )
                    {
                        if ( experimentalAlgorithm )
                            Console.WriteLine( "  attempt {0}: {1}  score '{2}'  remaining words {3}", g, guesses[ g ], scores[ g ],
                                               0 == remainingGuesses[ g ] ? dictionary.Count() : remainingGuesses[ g ] );
                        else
                            Console.WriteLine( "  attempt {0}: {1}  score '{2}'", g, guesses[ g ], scores[ g ] );
                    }
                }
            }
        } );

        successesResult = successes;
        failuresResult = failures;
        attemptsResult = attempts;
    } //SolveForAllWords

    static void Main( string[] args )
    {
        bool testActual = false;
        bool verbose = false;
        bool verboseSuccess = false;
        bool oneCore = false;
        bool randomizeDictionary = true;
        bool interactiveMode = false;
        bool playWordleMode = false;
        bool experimentalAlgorithm = false;
        bool bestGuess = true;
        bool firstWordMode = false;
        bool showWordCount = false;
        int firstWordIterations = 4;
        string firstGuess = defaultGuesses[ wordLen ];
        string userSolution = null;
        int maxAllowedGuesses = maxGuesses;

        for ( int i = 0; i < args.Length; i++ )
        {
            if ( '-' == args[i][0] || '/' == args[i][0] )
            {
                string argUpper = args[i].ToUpper();
                string arg = args[i];
                char c = argUpper[1];

                if ( 'A' == c )
                    testActual = true;
                else if ( 'D' == c )
                    showWordCount = true;
                else if ( 'F' == c )
                {
                    firstWordMode = true;

                    if ( arg.Length >= 4 && ':' == arg[ 2 ] )
                        firstWordIterations = int.Parse( arg.Substring( 3 ) );
                }
                else if ( 'G' == c )
                {
                    if ( arg.Length >= 4 && ':' == arg[ 2 ] )
                        firstGuess = arg.Substring( 3 ).ToLower();
                    else
                        Usage( "-g parameter has invalid value" );
                }
                else if ( 'I' == c )
                    interactiveMode = true;
                else if ( 'L' == c )
                {
                    if ( arg.Length > 5 || arg.Length < 4 )
                        Usage( "invalid /m argument" );

                    wordLen = int.Parse( arg.Substring( 3 ) );
                    if ( wordLen < 1 || wordLen > 15 )
                         Usage( "argument for /l is out of range" );

                    firstGuess = defaultGuesses[ wordLen ];
                }
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
                else if ( 'X' == c )
                {
                    experimentalAlgorithm = true;
                    bestGuess = ( 'x' == arg[ 1 ] );
                }
                else
                    Usage( "invalid argument: " + args[i] );
            }
            else
                Usage( "no argument flag - or / specified" );
        }

        if ( firstGuess.Length != wordLen )
            Usage( " specified first guess '" + firstGuess + "' doesn't match the word length of " + wordLen );

        if ( testActual && ( null != userSolution ) )
            Usage( " -a and -s are mutually exclusive" );

        if ( playWordleMode && interactiveMode )
            Usage( " -p and -i are mutually exclusive" );

        if ( playWordleMode && firstWordMode )
            Usage( " -p and -f are mutually exclusive" );

        if ( interactiveMode && firstWordMode )
            Usage( " -i and -f are mutually exclusive" );

        string allgreen = new string( 'g', wordLen );

        List<string> dictionary = new List<string>();

        try
        {
            foreach ( string line in System.IO.File.ReadLines( dictionaryFile ) )
            {
                if ( wordLen == line.Length )
                {
                    string lowerLine = line.ToLower();
                    if ( IsAllAlpha( lowerLine ) )
                       dictionary.Add( lowerLine );
                }
            }
        }
        catch ( Exception e )
        {
            Console.WriteLine( "Can't find the dictionary file {0} -- make sure it's in the same folder as wordle.exe", dictionaryFile );
            Environment.Exit( 1 );
        }

        if ( randomizeDictionary )
            RandomizeList<string>( dictionary );

        if ( showWordCount )
            Console.WriteLine( "The dictionary has {0} words of length {1}", dictionary.Count, wordLen );

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
            if ( experimentalAlgorithm )
                Console.WriteLine( "guess 0: {0}    -- {1} matching words remain", firstGuess, dictionary.Count() );
            else
                Console.WriteLine( "guess 0: {0}", firstGuess );
            int currentGuess = 0;
            string [] scores = new string[ maxAllowedGuesses ];
            string [] guesses = new string[ maxAllowedGuesses ];
            guesses[ 0 ] = firstGuess;
            int nextGuess = startingGuess;
            char [] score = new char[ wordLen ];
            bool [] slotsUsed = new bool[ wordLen ];
            List<string> validGuesses = experimentalAlgorithm ? new List<string>() : null;

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

                string attempt = experimentalAlgorithm ? FindNextAttempt2( ref nextGuess, guesses, scores, score, slotsUsed, dictionary,
                                                                           currentGuess, startingGuess, validGuesses, bestGuess ) :
                                                         FindNextAttempt( ref nextGuess, guesses, scores, score, slotsUsed, dictionary,
                                                                          currentGuess, startingGuess );
                guesses[ currentGuess ] = attempt;
                if ( experimentalAlgorithm )
                    Console.WriteLine( "guess {0}: {1}    -- {2} matching words remain", currentGuess, attempt, validGuesses.Count() );
                else
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
            "prick", "wince", "crimp", "knoll", "sugar", "whack", "mount", "perky", "could", "wrung",
            "light", "those", "moist", "shard", "pleat", "aloft", "skill", "elder", "frame", "humor",
            "pause", "elves", "ultra", "robin", "cynic", "aroma", "caulk", "shake", "dodge", "swill",
            "tacit", "other", "thorn", "trove", "bloke", "vivid", "spill", "chant", "choke", "rupee",
            "nasty", "mourn", "ahead", "brine", "cloth", "hoard", "sweet", "month", "lapse", "watch",
            "today", "focus", "smelt", "tease", "cater", "movie", "saute", "allow", "renew", "their",
            "slosh", "purge", "chest", "depot", "epoxy", "nymph", "stove", "lowly", "snout", "trope",
            "fewer", "shawl", "natal", "comma", "foray", "scare", "stair", "black", "squad", "royal",
            "chunk", "mince", "shame", "cheek", "ample", "flair", "foyer", "cargo", "oxide", "plant",
            "globe", "inert", "askew", "heist", "shown", "zesty", "trash", "larva", "forgo", "story",
            "hairy", "train", "homer", "badge", "midst", "canny", "shine", "gecko", "farce", "slung",
            "tipsy", "metal", "yield", "delve", "being", "scour", "glass", "gamer", "scrap", "boney",
            "hinge", "album", "vouch", "asset", "tiara", "crept", "bayou", "atoll", "manor", "creak",
            "showy", "phase", "froth", "depth", "gloom", "flood", "trait", "girth", "piety", "goose",
            "float", "donor", "atone", "primo", "apron", "blown", "cacao", "loser", "input", "gloat",
            "awful", "brink", "smite", "beady", "rusty", "retro", "droll", "gawky", "pinto", "egret",
            "lilac", "sever", "field", "fluff", "agape", "stead", "berth", "madam", "night", "bland",
            "liver", "wedge", "roomy", "wacky", "flock", "angry", "trite", "aphid", "tryst", "midge",
            "power", "elope", "cinch", "motto", "stomp", "upset", "bluff", "cramp", "quart", "coyly",
            "youth", "rhyme", "buggy", "alien", "smear", "unfit", "patty", "cling", "glean", "label",
            "hunky", "khaki", "poker", "gruel", "twice", "twang", "shrug", "treat", "waste", "merit",
            "woven", "needy", "clown", "irony", 
        };

        string [] userSolutions = { userSolution };
        IList<string> testCases = ( null != userSolution ) ? ( IList<string> ) userSolutions :
                                  testActual ? ( IList<string> ) actualSolutions : dictionary;

        Stopwatch stopWatch = Stopwatch.StartNew();

        if ( firstWordMode )
        {
            SortedList<string,int> bestList = new SortedList<string,int>();

            // Iterate more than once because results will be somewhat random depending on dictionary word order

            for ( int iteration = 0; iteration < firstWordIterations; iteration++ )
            {
                if ( randomizeDictionary )
                    RandomizeList<string>( dictionary );
    
                Console.WriteLine( "iteration {0}", iteration );

                // For each word in the dictionary as a starting guess, solve for each word in testCases

                for ( startingGuess = 0; startingGuess < dictionary.Count; startingGuess++ )
                {
                    int successes = 0, failures = 0, attempts = 0;
                    SolveForAllWords( testCases, startingGuess, dictionary, ref successes, ref failures, ref attempts,
                                      experimentalAlgorithm, oneCore, maxAllowedGuesses, bestGuess, allgreen, verboseSuccess, verbose );

                    if ( bestList.ContainsKey( dictionary[ startingGuess ] ) )
                        bestList[ dictionary[ startingGuess ] ] += successes;
                    else
                        bestList.Add( dictionary[ startingGuess ], successes );
                }
            
                WordCount [] awc = new WordCount[ dictionary.Count ];
                int iwc = 0;
                foreach ( KeyValuePair<string,int> pair in bestList )
                    awc[ iwc++ ] = new WordCount( (string) pair.Key, (int) pair.Value );
            
                WordCountComparer comparer = new WordCountComparer();
                Array.Sort( awc, comparer );
        
                Console.WriteLine( "  best:" );
                for ( int i = 0; i < Math.Min( awc.Count(), 10 ); i++ )
                    Console.WriteLine( "    {0} {1:.00}", awc[i].word, 100.0 * (float) awc[i].count / (float) ( testCases.Count * ( iteration + 1 ) ) );
            
                Console.WriteLine( "  worst:" );
                for ( int i = dictionary.Count - 1; i > Math.Max( -1, dictionary.Count - 10 ); i-- )
                    Console.WriteLine( "    {0} {1:.00}", awc[i].word, 100.0 * (float) awc[i].count / (float) ( testCases.Count * ( iteration + 1 ) ) );

                Console.WriteLine( "  time so far: {0} seconds", stopWatch.ElapsedMilliseconds / 1000 );
            }
        }
        else
        {
            int successes = 0, failures = 0, attempts = 0;
            SolveForAllWords( testCases, startingGuess, dictionary, ref successes, ref failures, ref attempts,
                              experimentalAlgorithm, oneCore, maxAllowedGuesses, bestGuess, allgreen, verboseSuccess, verbose );

            Console.WriteLine( "total games {0}, successes {1}, failures {2}, average attempts {3:.00}, success rate {4:.00}%, {5} milliseconds, first guess {6}",
                               testCases.Count(), successes, failures, (float) attempts / (float) testCases.Count(),
                               100.0 * (float) successes / (float) testCases.Count(), stopWatch.ElapsedMilliseconds, firstGuess );
        }
    } //Main
} //Wordle
