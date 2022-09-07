#
# Solves wordle most of the time
#

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

def find_new_letters( next_guess, prior_score, prior_guess ):
    # retain letters that aren't known correct -- 'g'

    new_letters = []
    for n in range( 0, 5 ):
        if 'g' != prior_score[ n ]:
            new_letters.append( next_guess[ n ] )

    # remove duplicate letters
    new_letters = [ *set( new_letters ) ]

    return new_letters

# when solving wordle for all possible words, this version on average finds solutions faster and more often

def find_next_attempt_smart( guesses, scores, dictionary, current_guess, starting_guess_index, valid_guesses ):
    if 0 == current_guess:
        return dictionary[ next_guess_index ]

    if 1 == current_guess:                   # add words that match the first word
        first_guess = guesses[ 0 ]
        first_score = scores[ 0 ]

        for i in range( 0, len( dictionary ) ):
            word = dictionary[ i ]

            if word != first_guess:
                score = calc_score( word, first_guess )
                if score == first_score:
                    valid_guesses.append( word )
    else:                                    # remove words that no longer fit given the latest score
        latest_index = current_guess - 1
        i = 0

        while i < len( valid_guesses ):
            word = valid_guesses[ i ]
            remove = word == guesses[ latest_index ]  # remove the most recent guess

            if remove == False:
                score = calc_score( word, guesses[ latest_index ] )
                remove = score != scores[ latest_index ]

            if remove:
                del valid_guesses[ i ]
            else:
                i += 1

    if 0 == len( valid_guesses ):
        print( "Error: the solution word isn't in the dictionary" )
        quit()

    # if there are 2 or fewer remaining words, pick one at random

    if len( valid_guesses ) <= 2:
        return valid_guesses[ 0 ]

    # record how frequently each letter is found in the set of valid guesses

    letter_counts = [ 0 ] * 26    # a..z
    last_guess = current_guess - 1

    for i in range( 0, len( valid_guesses ) ):
        for l in range( 0, 5 ):
            if 'g' != scores[ last_guess ][ l ]:
                letter_counts[ ord( valid_guesses[ i ][ l ] ) - ord( 'a' ) ] += 1

    # Score each valid guess based on how well each letter divides the space of remaining words
    # Letters found in half the words are best. Letters found once or in every word are worst.
    # Distance in n-dimensions is a sum of squares of distance in each dimension.
    # Don't bother finding the square root of the final distance, since it won't change anything.

    half_words = len( valid_guesses ) / 2
    best_score = 1000000000
    best_word = 0

    for i in range( 0, len( valid_guesses ) ):
        new_letters = find_new_letters( valid_guesses[ i ], scores[ last_guess ], guesses[ last_guess ] )
        char_count = len( new_letters )
        word_score = 0

        for l in range( 0, char_count ):
            distance = half_words - letter_counts[ ord( new_letters[ l ] ) - ord( 'a' ) ]
            word_score += ( distance * distance )

        # penalize non-unique letters and 'g' letters

        for l in range( char_count, 5 ):
            word_score += ( half_words * half_words )

        # find the word with the lowest score. score is based on closeness to the center of all dimensions

        if word_score < best_score:
            #print( "new best word with score ", valid_guesses[ i ], word_score )
            best_word = i
            best_score = word_score

    return valid_guesses[ best_word ]

def main():
    global next_guess_index

    with open( "words5.txt", "r" ) as dictionary_file:
        dictionary = dictionary_file.read().splitlines()

    dictionary.remove( "" )
    dictionary_len = len( dictionary )
    print( "dictionary has ", dictionary_len, " 5 letter words" )
    
    guess = "patch"    # after exhaustive testing for all solutions and all startings words, this works best
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
    valid_guesses = []
    use_smart_algorithm = True
    
    while current_guess < 6:
        print( "enter score:", end = " " )
        score_given = input()
    
        if validate_score( score_given ):
            scores.append( score_given )
            current_guess += 1

            if "ggggg" == score_given:
                print( "success in", current_guess, "tries!" )
                quit()

            if use_smart_algorithm:
                attempt = find_next_attempt_smart( guesses, scores, dictionary, current_guess, starting_guess_index, valid_guesses )
                print( "guess #", current_guess + 1, attempt, "   --- ", len( valid_guesses ), " words remaining" )
            else:
                attempt = find_next_attempt( guesses, scores, dictionary, current_guess, starting_guess_index )
                print( "guess #", current_guess + 1, attempt )

            guesses.append( attempt )
    
    print( "ran out of attempts :(" )

if __name__ == "__main__":
    main()


