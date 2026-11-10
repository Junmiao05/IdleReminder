# Rebuild five original mono PCM tones: seven one-second pulses each.
$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
$rate=44100
$seconds=7
for($j=0;$j -lt 5;$j++){
 $pulse=[byte[]]::new($rate*2)
 $frequency=@(1046.5,880,523.25,1400,660)[$j]
 for($i=0;$i -lt $rate;$i++){
  $t=$i/[double]$rate
  $envelope=[Math]::Min(1,$t*60)*[Math]::Exp(-$t*5)
  # Fade each pulse's last 20 ms to avoid clicks at repetition boundaries.
  $envelope*=[Math]::Min(1,($rate-1-$i)/($rate*0.02))
  if($j -eq 3){$envelope*=[Math]::Pow([Math]::Sin($t*12),2)}
  $sample=[int16]([Math]::Sin(2*[Math]::PI*$frequency*$t)*$envelope*12000)
  $bytes=[BitConverter]::GetBytes($sample)
  $pulse[$i*2]=$bytes[0];$pulse[$i*2+1]=$bytes[1]
 }
 $path=Join-Path $PSScriptRoot "assets\tone$j.wav"
 $writer=[IO.BinaryWriter]::new([IO.File]::Create($path))
 try{
  $size=$pulse.Length*$seconds
  $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF'));$writer.Write([int](36+$size))
  $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt '));$writer.Write([int]16)
  $writer.Write([int16]1);$writer.Write([int16]1);$writer.Write([int]$rate)
  $writer.Write([int]($rate*2));$writer.Write([int16]2);$writer.Write([int16]16)
  $writer.Write([Text.Encoding]::ASCII.GetBytes('data'));$writer.Write([int]$size)
  for($n=0;$n -lt $seconds;$n++){$writer.Write($pulse)}
 }finally{$writer.Dispose()}
}
