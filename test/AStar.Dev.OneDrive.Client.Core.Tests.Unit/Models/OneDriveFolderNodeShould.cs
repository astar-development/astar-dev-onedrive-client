using AStar.Dev.OneDrive.Client.Core.Entities.Enums;
using AStar.Dev.OneDrive.Client.Models;

namespace AStar.Dev.OneDrive.Client.Core.Tests.Unit.Models;

public class OneDriveFolderNodeShould
{
    [Fact]
    public void InitializeWithDefaultValues()
    {
        var node = new OneDriveFolderNode();

        node.Id.ShouldBe(string.Empty);
        node.Name.ShouldBe(string.Empty);
        node.Path.ShouldBe(string.Empty);
        node.ParentId.ShouldBeNull();
        node.IsFolder.ShouldBeFalse();
        node.Children.ShouldBeEmpty();
        node.ChildrenLoaded.ShouldBeFalse();
        node.IsExpanded.ShouldBeFalse();
        node.SelectionState.ShouldBe(SelectionState.Unchecked);
        node.IsSelected.ShouldBeNull();
    }

    [Fact]
    public void InitializeWithProvidedValues()
    {
        var node = new OneDriveFolderNode("id123", "MyFolder", "/MyFolder", "parentId", true);

        node.Id.ShouldBe("id123");
        node.Name.ShouldBe("MyFolder");
        node.Path.ShouldBe("/MyFolder");
        node.ParentId.ShouldBe("parentId");
        node.IsFolder.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenSelectionStateChanges()
    {
        var node = new OneDriveFolderNode();
        var propertyChangedRaised = false;
        node.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(OneDriveFolderNode.SelectionState)) propertyChangedRaised = true;
        };

        node.SelectionState = SelectionState.Checked;

        propertyChangedRaised.ShouldBeTrue();
        node.SelectionState.ShouldBe(SelectionState.Checked);
    }

    [Fact]
    public void NotRaisePropertyChangedWhenSelectionStateSetToSameValue()
    {
        var node = new OneDriveFolderNode { SelectionState = SelectionState.Checked };
        var propertyChangedCount = 0;
        node.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(OneDriveFolderNode.SelectionState)) propertyChangedCount++;
        };

        node.SelectionState = SelectionState.Checked;

        propertyChangedCount.ShouldBe(0);
    }

    [Fact]
    public void RaisePropertyChangedWhenIsSelectedChanges()
    {
        var node = new OneDriveFolderNode();
        var propertyChangedRaised = false;
        node.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(OneDriveFolderNode.IsSelected)) propertyChangedRaised = true;
        };

        node.IsSelected = true;

        propertyChangedRaised.ShouldBeTrue();
        node.IsSelected.ShouldBe(true);
    }

    [Fact]
    public void NotRaisePropertyChangedWhenIsSelectedSetToSameValue()
    {
        var node = new OneDriveFolderNode { IsSelected = true };
        var propertyChangedCount = 0;
        node.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(OneDriveFolderNode.IsSelected)) propertyChangedCount++;
        };

        node.IsSelected = true;

        propertyChangedCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(SelectionState.Unchecked)]
    [InlineData(SelectionState.Checked)]
    [InlineData(SelectionState.Indeterminate)]
    public void SetAllSelectionStates(SelectionState state)
    {
        var node = new OneDriveFolderNode
        {
            SelectionState = state
        };

        node.SelectionState.ShouldBe(state);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void SetAllIsSelectedValues(bool? value)
    {
        var node = new OneDriveFolderNode
        {
            IsSelected = value
        };

        node.IsSelected.ShouldBe(value);
    }

    [Fact]
    public void AllowAddingChildNodes()
    {
        var parent = new OneDriveFolderNode("parent", "Parent", "/Parent", null, true);
        var child = new OneDriveFolderNode("child", "Child", "/Parent/Child", "parent", true);

        parent.Children.Add(child);

        parent.Children.Count.ShouldBe(1);
        parent.Children[0].ShouldBe(child);
    }

    [Fact]
    public void SupportObservableChildrenCollection()
    {
        var parent = new OneDriveFolderNode();
        var collectionChangedRaised = false;
        parent.Children.CollectionChanged += (_, _) => collectionChangedRaised = true;

        parent.Children.Add(new OneDriveFolderNode());

        collectionChangedRaised.ShouldBeTrue();
    }
}
