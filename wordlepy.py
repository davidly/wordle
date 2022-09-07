next_guess_index = 0

def validate_score( score ):
    if len( score ) != 5:
        print( "score must be 5 characters of space, y or g" )
        return False

    for c in score:
        if c != 'g' and c != 'y' and c != ' ':
            print( "score must be 5 characters of space, y or g" )
            return False

    return True

def calc_score( solution, guess ):
    score = list( "     " )
    slot_used = [ False ] * 5

    for i in range( 0, 5 ):
        if guess[ i ] == solution[ i ]:
            score[ i ] = 'g'
            slot_used[ i ] = True

    for i in range( 0, 5 ):
        if 'g' != score[ i ]:
            for j in range( 0, 5 ):
                if i != j and slot_used[ j ] == False and guess[ i ] == solution[ j ]:
                    score[ i ] = 'y'
                    slot_used[ j ] = True
                    break;

    return "".join( score )

def find_next_attempt( guesses, scores, dictionary, current_guess, starting_guess_index ):
    global next_guess_index

    while True:
        attempt = dictionary[ next_guess_index ]
        # print( "fna: attempt ", attempt )
        next_guess_index += 1
        if next_guess_index == len( dictionary ):
            next_guess_index = 0

        guess_matches = True

        for g in range( 0, current_guess ):
            #print( "  checking ", attempt, "for g ", g, " which was guess ", guesses[ g ], " with score ", scores[ g ] )
            score = calc_score( attempt, guesses[ g ] )
            #print( "    calculated score:'", score, "'", "compared with: '", scores[ g ], "'" )

            if score != scores[ g ]:
                guess_matches = False
                break;

        if guess_matches == True:
            break;

        if next_guess_index == starting_guess_index:
            print( "Error: the solution word isn't in the dictionary" )
            quit()

    return attempt

def main():
    global next_guess_index

    with open( "words5.txt", "r" ) as dictionary_file:
        dictionary = dictionary_file.read().splitlines()

    dictionary.remove( "" )
    dictionary_len = len( dictionary )
    print( "dictionary has ", dictionary_len, " 5 letter words" )
    
    guess = "patch"
    starting_guess_index = dictionary.index( guess )
    next_guess_index = starting_guess_index
    next_guess_index += 1
    if next_guess_index >= dictionary_len:
        next_guess_index = 0

    print( "guess # 1", guess )
    guesses = []
    guesses.append( guess )
    scores = []
    current_guess = 0
    
    while current_guess < 6:
        print( "enter score:", end = " " )
        score_given = input()
    
        if validate_score( score_given ):

            scores.append( score_given )
            current_guess += 1

            if "ggggg" == score_given:
                print( "success in", current_guess, "tries!" )
                quit()

            attempt = find_next_attempt( guesses, scores, dictionary, current_guess, starting_guess_index )
            print( "guess #", current_guess + 1, attempt )
            guesses.append( attempt )
    
    print( "ran out of attempts :(" )

if __name__ == "__main__":
    main()


