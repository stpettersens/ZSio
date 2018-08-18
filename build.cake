var target = Argument("target", "Default");

Task("Default")
.Does(() => 
{
	MSBuild("./ZSio.sln");
});

RunTarget(target);
