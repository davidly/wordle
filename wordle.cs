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

    static string Score( string solution, string guess )
    {
        char [] s = new char[ wordLen ];
        bool [] slotUsed = new bool[ wordLen ];

        for ( int c = 0; c < wordLen; c++ )
        {
            s[ c ] = ' ';
            slotUsed[ c ] = false;
        }

        for ( int i = 0; i < wordLen; i++ )
        {
            if ( guess[ i ] == solution[ i ] )
            {
                s[ i ] = 'g';
                slotUsed[ i ] = true;
            }
        }

        for ( int i = 0; i < wordLen; i++ )
        {
            if ( guess[ i ] != solution[ i ] )
            {
                for ( int j = 0; j < wordLen; j++ )
                {
                    if ( i != j && ! slotUsed[ j ] && guess[ i ] == solution[ j ] )
                    {
                        slotUsed[ j ] = true;
                        s[ i ] = 'y';
                    }
                }
            }
        }

        return new string( s );
    } //Score

    static void Main( string[] args )
    {
        Random rand = new Random( Environment.TickCount );
        List<string> dictionary = new List<string>();
        foreach ( string line in System.IO.File.ReadLines( @"words.txt" ) )
            if ( wordLen == line.Length  &&
                 -1 == line.IndexOf( '.' ) &&
                 -1 == line.IndexOf( '-' ) &&
                 -1 == line.IndexOf( '\'' ) )
               dictionary.Add( line.ToLower() );

        for ( int r = 0; r < dictionary.Count(); r++ )
        {
            int x = rand.Next( dictionary.Count() );
            int y = rand.Next( dictionary.Count() );
            string t = dictionary[ x ];
            dictionary[ x ] = dictionary[ y ];
            dictionary[ y ] = t;
        }

        int successes = 0;
        int failures = 0;
        string allgreen = new string( 'g', wordLen );

        //for ( int isolution = 0; isolution < dictionary.Count(); isolution++ )
        Parallel.For ( 0, dictionary.Count(), isolution =>
        {
            string [] scores = new string[ maxGuesses ];
            string [] guesses = new string[ maxGuesses ];

            string solution = dictionary[ isolution ];
            //Console.WriteLine( "solution: {0}", solution );

            bool success = false;
            int nextGuess = rand.Next( dictionary.Count() );

            for ( int i = 0; i < maxGuesses; i++ )
            {
                string attempt = null;

                do
                {
                    attempt = dictionary[ nextGuess++ ];
                    if ( nextGuess >= dictionary.Count() )
                        nextGuess = 0;
                    bool newGuessMatchesPriorGuesses = true;
                    //Console.WriteLine( "  guess {0}", attempt );
    
                    for ( int g = 0; g < i; g++ )
                    {
                        string guessScore = Score( attempt, guesses[ g ] );
                        //Console.WriteLine( "    match of guess {0}: {1}", g, guessScore );
                        if ( 0 != String.Compare( guessScore, scores[ g ] ) )
                        {
                            newGuessMatchesPriorGuesses = false;
                            break;
                        }
                    }
    
                    if ( newGuessMatchesPriorGuesses )
                        break;
                } while( true );
    
                string attemptScore = Score( solution, attempt );
                //Console.WriteLine( "  attempt {0} has score {1}", attempt, attemptScore );
                scores[ i ] = attemptScore;
                guesses[ i ] = attempt;
                if ( 0 == String.Compare( attemptScore, allgreen ) )
                {
                    success = true;
                    break;
                }
            }

            if ( success )
                Interlocked.Increment( ref successes );
            else
                Interlocked.Increment( ref failures );
        } );

        Console.WriteLine( "total games {0}, successes {1}, failures {2}, rate {3}", dictionary.Count(), successes, failures, (float) successes / (float) dictionary.Count() );
    } //Main
} //Wordle
