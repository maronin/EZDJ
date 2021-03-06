#EZDJ 

![](http://i.imgur.com/iKHw90g.png)

##Description

This is a project that was inspired by wanting to share music with my friends while I played video games with them. This application is written in C# and uses an open source audio and MIDI library called [NAudio](https://naudio.codeplex.com/).

This project was particularly challenging, as it involved figuring out a way to mix two input sources (my microphone and the music I was playing) into one source (what my friends where hearing) and at the same time, outputting the music to myself as well.

## Instructions on how to use

### Adding songs
Click on the 'note' and 'plus' icon located on the middle right side of the application. Proceed to add songs (Supports: .wave, .mp3, .aiff).

### Playing song
Once the songs have been added, they will be displayed at the bottom of the application. Scroll up or down to view your tracks if you have more than at least 14 songs. To play the track, simply double click the song you want to play. 

### Changing volume
Move the circular blue bars to adjust volume. The one located on the left is how loud the volume is for you. The one located on the right is the volume for others.

### Seek track
The large circular green bar in the middle is used to seek the current song. Click anywhere along the circle or drag accordingly. 

### Setup sources
In order for this to work and be able to mix your mic and music to a single output source, you'll need to download [Virtual Audio Cable](http://software.muzychenko.net/eng/vac.htm). Once you've installed virtual audio cable, you can follow the setup below.

- Set the **input** to your mic
- The **output** to your _Virtual Audio Cable (eg. Line 1)_ 
- The **music output** to _Virtual Audio Cable (eg. Line 1)_

For the application you are going to use to communicate with your freinds (Skype, Discord, etc.), select Line 1 as your input source.

Click the setting icons located on the top left corner of the application  to access the input/output settings.

![](http://i.imgur.com/uiF1aYj.png)


