var maxSuggestions = 10;

function findSuggestions(searchText) {
    if (!searchText)
        return "";

    var letter = searchText.charAt(0);
    var index = null;
    for (var i = 0; i < suggestionIndex.length; i++) {
        if (suggestionIndex[i].letter == letter) {
            index = suggestionIndex[i];
            break;
        }
    }
    if (!index)
        return "";

    var result = "";
    var suggestionsCount = 0;
    var isHit = false;
    for (var i = index.lowerIndex; i <= index.upperIndex; i++) {
        // This is the fastest alternative of "StartsWith" check
        if (suggestionWords[i].lastIndexOf(searchText, 0) === 0) {
            result += suggestionWords[i] + '\n';
            isHit = true;
            suggestionsCount++;
            if (suggestionsCount >= maxSuggestions)
                break;
        } else {
            // Performance optimization:
            // The words are sorted so all matched words should be located one after another.
            if (isHit)
                break;
        }
    }

    return result;
}

function WordIndex(letter, lowerIndex, upperIndex) {
    this.letter = letter;
    this.lowerIndex = lowerIndex;
    this.upperIndex = upperIndex;
}

var suggestionIndex = [];
var suggestionWords = [];