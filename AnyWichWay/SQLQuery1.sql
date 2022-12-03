Select TOP 200 * FROM dbo.Sandwiches
WHERE	
	Sweet >= Sweet AND
	Sweet > Salty AND
	Sweet > Sour AND
	Sweet > Bitter AND
	Sweet > Hot AND
	[Exp] > Egg AND
	[Exp] > Catching AND
	[Exp] >= [Exp] AND
	[Exp] > Raid AND
	[Exp] > ItemDrop AND
	[Exp] > Humungo AND
	[Exp] > Teensy AND
	[Exp] > Encounter AND
	[Exp] > Title AND
	[Exp] > Sparkling AND
	Normal >= Normal AND
	Normal > Fighting AND
	Normal > Flying AND
	Normal > Poison AND
	Normal > Ground AND
	Normal > Rock AND
	Normal > Bug AND
	Normal > Ghost AND
	Normal > Steel AND
	Normal > Fire AND
	Normal > Water AND
	Normal > Grass AND
	Normal > Electric AND
	Normal > Psychic AND
	Normal > Ice AND
	Normal > Dragon AND
	Normal > Dark AND
	Normal > Fairy AND
	Normal + Encounter > 0
ORDER BY Cost ASC
