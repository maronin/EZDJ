#EZDJ 

![](http://i.imgur.com/iKHw90g.png)

##Description

This is a project that was inspired by wanting to share music with my friends while I played video games with them. This application is written in C# and uses an open source audio and MIDI library called [NAudio](https://naudio.codeplex.com/).

This project was particularly challenging, as it involved figuring out a way to mix two input sources (my microphone and the music I was playing) into one source (what my friends where hearing) and at the same time, outputting the music to myself as well.

## Instructions on how to use
### Adding songs
Click on the 'note' and 'plus' icon located on the middle right side of the application.
### Changing volume
Move the ciruclar blue bars to adjust volume. The one located on the left is how loud the volume is for you. The one located on the right is the volume for others.
### Setup sources
You'll need to download [Virtual Audio Cable](http://software.muzychenko.net/eng/vac.htm) in order for this set up to work. Once you've installed virtual audio cable, you can follow the setup below.

- Set the **input** to your mic
- The **output** to your _Virtual Audio Cable (Line 1)_ 
- The **music output** to _Virtual Audio Cable (Line 1)_

For the application you are going to use to communicate with your freinds (Skype, Discord, etc.), select Line 1 as your input source.

Click the setting icons located on the top left corner of the application  to access the input/output settings.

![](http://i.imgur.com/uiF1aYj.png)


