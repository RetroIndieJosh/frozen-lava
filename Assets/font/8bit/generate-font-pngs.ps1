param (
        [int]$pointsize = 300
)

for($i = 32; $i -le 126; $i++) { 
        $letter = [char] $i
        $file = $letter + ".png"
        $label = "label:`"" + $letter + "`""
        $cmd = "magick convert -background none -fill black -font `"8bit v2.ttf`" -pointsize $pointsize $label $file"
        echo $cmd
        iex $cmd
}
