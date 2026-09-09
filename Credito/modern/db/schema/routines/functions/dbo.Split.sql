
CREATE FUNCTION [dbo].[Split] ( @stringToSplit VARCHAR(MAX), @Separador VARCHAR(3)=',')
RETURNS
 @returnList TABLE ([Name] [nvarchar] (500))
AS
BEGIN

 DECLARE @name NVARCHAR(255)
 DECLARE @pos INT

 WHILE CHARINDEX(@Separador, @stringToSplit) > 0
 BEGIN
  SELECT @pos  = CHARINDEX(@Separador, @stringToSplit)  
  SELECT @name = SUBSTRING(@stringToSplit, 1, @pos-1)

  INSERT INTO @returnList 
  SELECT @name

  SELECT @stringToSplit = SUBSTRING(@stringToSplit, @pos+1, LEN(@stringToSplit)-@pos)
 END

 INSERT INTO @returnList
 SELECT @stringToSplit

 RETURN 
END

-- select * from dbo.Split('Chennai-Bangalore-Mumbai','-')
