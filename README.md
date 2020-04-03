# EDGEePubAnnotationMerger
Program to merge EDGE annotations into an ePub ebook

My wife had a long ebook which she was reading in EDGE and she made approximately 300 color hilights in it. 
After EDGE dropping support for the ePub format, I was asked to merge these annotations into the original ebook.

This program is absolutely not complete, an its current form it was able to transfer approximately 50% of the highligts. 
If you have a lot of books annotated books and time to spend on this, it can be improved. If you wat to use it, you will need to understand the code and addopt it to your needs.


So here is what I have done:

### Finding the annotation database in EDGE

I have restores a copy of the machine where my wife has used EDGE to a version where EDGE still could read epub to a virtual machine, and disabled windows updates, to have a playground to experience.
Based on https://answers.microsoft.com/en-us/edge/forum/all/where-are-edge-epub-annotationshighlights-stored/02df402b-25bb-4c69-a246-e12ad8c7dbb3 this I was looking for the 
spartan.edb in %LocalAppData%\Packages\Microsoft.MicrosoftEdge_8wekyb3d8bbwe

When I had the database, used http://www.nirsoft.net/utils/esedatabaseview.zip to export the annotations database.

### Interpretting the annotation database

The annotations are relative to the ePub document, which in fact is a zipped archive comtaining HTML files. The contnt of the book is typically in several HTML files, like one file per chapter.

The important fields for this project are:

1. Context - the higlighted text
2. HighlightColor - the color of the highlight
3. ReadingPosition - the position of the highlight of the text

The first two are trivial, but the position is difficult to guess. After some studying I came to the following conclusion:



Here are two typical formats: 

"/6/20!/4/22,/5:439,/5:505"
"/6/20!/4/42/3,:270,:362"

/6/20! is the file specification, /6 was contant in my case, and I could not figure out, how 20! is translating to 009.html. What I saw, that the next document was 22 the next 24 so it looks like every second number is corresponding to a document.

/4/22 or /4/42 is the parragraph specification. /4 is always constant the figure after that is two times the number of the parragraph. for counting the parragraphs I could use <h? and <p parragraphs.

The end is the position within the parragraph, /5 means fifth section and all <span>-s count as one section and all text between them count as one section.


### Automatic processing

To process the annotations I have used the following algorythm:

1. Export RowId,Context, HighlightColor and ReadingPosition to a colon separated text file
2. Convert it to UTF-8
3. Read in the file and store it into a "annotation" List
4. Interpret the postition and where only one section is given add it to the second.
5. Throw out the lines, where we could not interpret the position format. My assumptions about the format were right for most of the cases but arround 10% of the position was given in a different format.
6. Find the files matching the file number. Simply load the file, fetch the text at the given position and if it matches the context, then this is the valid file.
7. Add in the css for the book the note_yellow, note_blue and note_green classes with the corresponding background colors.
8. For each found file for all annotations insert </span> after and <span class="note_yellow"> before the found start and end position. In order not to overwrite the positions of the annotation which are later in the file, I have sorted all annotations based on their position in reverse order.
9. Save the file to a new name
10. After processing all files, print the list of non processed annotations.
11. After everything is ready manually correct the rest with Sigil or Calibre

## Further improvement possibilities

1. The non matching positions can be analysed to see if the ycan be understud
2. At the moment if in the found annotations are <span> parts then it is scipped, this can be improved to handle it correctly and insert coloring spans as needed.
3. Before making changes check if the text of the selection matches the context from the database.


