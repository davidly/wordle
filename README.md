# wordle
wordle generation and solution app

Download https://raw.githubusercontent.com/dwyl/english-words/master/words.txt into the same folder as wordle.exe

Build like this (using your favorite version of .net)

c:\windows\microsoft.net\framework64\v4.0.30319\csc.exe wordle.cs /nowarn:0162 /nowarn:0168 /nologo /debug:full /o+ /d:DEBUG

The apps finds all 5-character words in words.txt and for each attempts to solve wordle. When it can't find a solution, it's
because there are multiple answers that fit.
