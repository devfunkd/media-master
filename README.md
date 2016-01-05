# Media Master #

## Release Notes ##
* (Version 1.0) (Development)

## Source Code ##
#### Git Repository ####
git clone https://github.com/devfunkd/media-master.git

#### Commit Strategy ####
gitflow: https://www.atlassian.com/git/tutorials/comparing-workflows/feature-branch-workflow

# Standards & Guidelines #

## Overview ##
In order to keep the code consistent, we follow certain conventions. Many of the choices we have made are somewhat arbitrary and could easily have gone another way. At this point, most of these conventions are already well-established, so please don't re-open a discussion about them unless you have new issues to present.

## General - Please Read This! ##
Follow these guidelines unless you have an extremely good reason not to. Add a comment explaining why you are not following them so others will know your reasoning.

Don't make arbitrary changes in existing code merely to conform them to these guidelines. Normally, you should only change the parts of a file that you have to edit in order to fix a bug or implement new functionality. In particular, don't use automatic formatting to change an entire file at once as this makes it difficult to identify the underlying code changes when we do a code review.

In cases where we make broad changes in layout or naming, they should be committed separately from any bug fixes or feature changes in order to keep the review process as simple as possible. That said, we don't do this very often, since we have real work to do!


## Layout ##

### Namespace, Class, Structure, Interface, Enumeration and Method Definitions ###

Place the opening and closing braces on a line by themselves and at the same level of indentation as their parent.


```
#!c#

public class MyClass : BaseClass, SomeInterface
{
    public void SomeMethod(int num, string name)
    {
        // code of the method
    }
}
```

An exception may be made if a method body or class definition is empty


```
#!c#

public virtual void SomeMethod(int num, string name) { }
public class GadgetList : List<Gadget> { }
```


### Property Definitions ###

Prefer automatic backing variables wherever possible


```
#!c#

public string SomeProperty { get; private set; }
```

If a getter or setter has only one statement, a single line should normally be used


```
#!c#

public string SomeProperty 
{
    get { return _innerList.SomeProperty; }
}
```

If there is more than one statement, use the same layout as for method definitions


```
#!c#

public string SomeProperty
{
    get
    {
        if (_innerList == null)
            InitializeInnerList();

        return _innerList.SomeProperty;
    }
}
```

### Spaces ###

Method declarations and method calls should not have spaces between the method name and the parenthesis, nor within the parenthesis. Put a space after a comma between parameters.


```
#!c#

public void SomeMethod(int x, int y)
{
    Console.WriteLine("{0}+{1}={2}", x, y, x+y );
}
```

Control flow statements should have a space between the keyword and the parenthesis, but not within the parenthesis.


```
#!c#

for (int i = 1; i < 10; i++)
{
    // Do Something
}
```

There should be no spaces in expression parenthesis, type casts, generics or array brackets, but there should be a space before and after binary operators.


```
#!c#

int x = a * (b + c);
var list = new List<int>();
list.Add(x);
var y = (double)list[0];
```

### Indentation ###

Use four consecutive spaces per level of indent. Don't use tabs - except where the IDE can be set to convert tabs to four spaces. In Visual Studio, set the tab size to 4, the indent size to 4 and make sure Insert spaces is selected.

Indent content of code blocks.

In switch statements, indent both the case labels and the case blocks. Indent case blocks even if not using braces.


```
#!c#

switch (name)
{
    case "John":
        break;
}
```

### Newlines ###

Methods and Properties should be separated by one blank line. Private member variables should have no blank lines.

Blocks of related code should have not have any blank lines. Blank lines can be used to visually group sections of code, but their should never be multiple blank lines.

If brackets are not used on a control flow statement with a single line, a blank line should follow.


```
#!c#

public static double GetAttribute(XmlNode result, string name, double defaultValue)
{
    var attr = result.Attributes[name];

    double attributeValue;
    if (attr == null || !double.TryParse(attr.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out attributeValue))
        return defaultValue;

    return attributeValue;
}
```

### Naming ###

The following table shows our naming standard for various types of names. All names should clear enough that somebody unfamiliar with the code can learn about the code by reading them, rather than having to understand the code in order to figure out the names. We don't use any form of "Hungarian" notation.

| Named Item | Naming Standard | Notes |
|------------|-----------------|-------|
| Namespaces | PascalCasing | |
| Types | PascalCasing | |
|  Methods | PascalCasing | |
| Properties | PascalCasing | | 
| Public Fields | PascalCasing | |
| Private, Protected and Internal Fields | _camelCasing | |
| Parameters | camelCasing | |
| Local Variables | camelCasing | |


### Use of the var keyword ###

The var keyword should be used where the type is obvious to someone reading the code, for example when creating a new object. Use the full type whenever the type is not obvious, for example when initializing a variable with the return value of a method.


```
#!c#

var i = 12;
var list = new List<int>();
Foo foo = GetFoo();
```

