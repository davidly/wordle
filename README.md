# wordle
wordle generation and solution app

A default words.txt is given. Or, Download https://raw.githubusercontent.com/dwyl/english-words/master/words.txt into the same folder as wordle.exe

Build like this (using your favorite version of .net)

    c:\windows\microsoft.net\framework64\v4.0.30319\csc.exe wordle.cs /nologo /debug:full /o+

By default, the app finds all 5-character words in words.txt and for each attempts to solve wordle. When it can't find a solution, it's
because there are multiple answers that fit.

Usage: wordle [-a] [-g:guess] [-i] [-o] [-r] [-s:solution] [-v]

    -a          Test against actual wordle solutions, not the whole dictionary
    -g:guess    The first guess word to use. Default is "glyph"
    -i          Interactive mode. Use this to have the app play wordle for you.
    -o          Use just one core
    -r          Randomize the order of words in the dictionary
    -s:solution The word to search for instead of the whole dictionary
    -v          Verbose logging of failures to find a solution. -V for successes too
    notes:      Assumes words.txt in the current folder contains a dictionary
                Only one of -a or -s can be specified
    samples:    wordle              solve each word in the dictionary
                wordle -i           interactive mode for use with the wordle app
                wordle -i -r        interactive mode, but randomize dictionary word order
                wordle -i /g:group  interactive mode, but make the first guess "group"
                wordle /s:tangy /V  solve for tangy and show the guesses
                wordle -a -v        solve for known wordle solutions and show failures.
                wordle -a -V        solve for known wordle solutions and show details.

 The code in wordle.cs is covered under GPL v3.
 
 The file words.txt comes originally from WordNet. 
 
     https://wordnet.princeton.edu/license-and-commercial-use
