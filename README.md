# Example in C#
You can run this version of OCR on any machine. The code will be self serving apart from import libraries.

## Libraries
prequisites are that you install/run the following:
+ .NET 8.0 [download here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (.NET 7.0 is also supported but current Target is 8.0)
+ SixLabors Image [download here](https://nuget.org/packages/SixLabors.ImageSharp) (used for multi platform image importing and RGB data extraction)
+ VS2022 (for .NET 7 and higher) [download community version here](https://visualstudio.microsoft.com/vs/community/)
  + To test a older version of VS (download here)[https://visualstudio.microsoft.com/vs/older-downloads/]

## Helpful links

+ Video based on the creation of this software in Python (original video) [watch here](https://www.youtube.com/watch?v=vzabeKdW9tE) 
  + this is only build up until the number part, the clothes algorithm is not included in this project 
+ MNIST datasets to download [visit here (SSL IS EXPIRED, open in compatible browser like firefox)](http://yann.lecun.com/exdb/mnist/)

## Retargeting .NET Core/Framework
This code can run on any version of C# all the way down to .NET Framework 2.0 if you exclude the Image import data and use the `#Windows` precompiler settings.
On Windows it will try to use `System.Drawing` for the code, this is why the inclusion of `#IF` Pre-processor lines are used.

To Debug similarly use the name `#DEBUGLOG` at the top of the script
