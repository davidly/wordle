# wordle
wordle generation and solution app

Build like this (using your favorite version of .net):

    c:\windows\microsoft.net\framework64\v4.0.30319\csc.exe wordle.cs /nologo /debug:full /o+

This app has several modes:

    * By default, the app finds all 5-character words in words.txt and for each attempts to solve wordle. When it can't find 
      a solution, it's because there are multiple answers that fit. Use -g to override the starting guess.
    * -i Interactive mode to have this app solve Wordle for you if you're playing online
    * -p Play wordle with randomly generated solutions
    * -f Like the default mode, but run it 4 times using every word in the dictionary as a starting word.

To use this app as a tool to solve a the day's game, use -i. To specifiy your own starting word, use -g:guess. 

    Usage: wordle [-a] [-g:guess] [-i] [-o] [-r] [-s:solution] [-v]
      -a          Test against actual wordle solutions, not the whole dictionary
      -f          Try every word as a first guess to see what works best/worst in several iterations.
      -g:guess    The first guess word to use. Default is "blimp"
      -i          Interactive mode. Use this to have the app play wordle for you.
      -m:X        Limit guesses to just X (2-12). Default is 6
      -o          Use just one core
      -p          Play wordle
      -r          Don't Randomize the order of words in the dictionary
      -s:solution The word to search for instead of the whole dictionary
      -v          Verbose logging of failures to find a solution. -V for successes too
      -x          Use experimental algorithm for finding the next guess. -X for worst guess.
      notes:      Assumes words.txt in the current folder contains a dictionary
                  Only one of -a or -s can be specified
                  Only one of -f, -i, or -p can be specified
                  -f allows the iteration count (default 4) to be specified with -f:X
      samples:    wordle              solve each word in the dictionary
                  wordle -i           interactive mode for use with the wordle app
                  wordle -i -r        interactive mode, but don't randomize dictionary word order
                  wordle -i /g:group  interactive mode, but make the first guess "group"
                  wordle /s:tangy /V  solve for tangy and show the guesses
                  wordle -a -v        solve for known wordle solutions and show failures.
                  wordle -a -V        solve for known wordle solutions and show details.

 The code in wordle.cs is covered under GPL v3.
 
 The file words.txt comes originally from WordNet. Some additional slang and Wordle solutions have been added.
 
     https://wordnet.princeton.edu/license-and-commercial-use
     
 For some thoughts on this app, see: 
 
     https://medium.com/@davidly_33504/first-guesses-in-wordle-291ebd50713a
     
 For more on the /x flag (it's better than choosing words at random!):
 
     https://medium.com/@davidly_33504/choosing-wordle-words-69a7ed5f966b
