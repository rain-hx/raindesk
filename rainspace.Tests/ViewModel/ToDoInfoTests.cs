using rainspace.ViewModel;

namespace rainspace.Tests.ViewModel;

[TestClass]
public class ToDoInfoTests
{
    [TestMethod]
    public void SettingSameTitle_DoesNotRaisePropertyChanged()
    {
        var todo = new ToDoInfo { Title = "Legacy task" };
        int changeCount = 0;
        todo.PropertyChanged += (_, _) => changeCount++;

        todo.Title = "Legacy task";

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingSameDescription_DoesNotRaisePropertyChanged()
    {
        var todo = new ToDoInfo { Description = "Details" };
        int changeCount = 0;
        todo.PropertyChanged += (_, _) => changeCount++;

        todo.Description = "Details";

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingDifferentDescription_RaisesDescriptionAndMsgChanges()
    {
        var todo = new ToDoInfo();
        var propertyNames = new List<string?>();
        todo.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        todo.Description = "Details";

        CollectionAssert.AreEqual(
            new[] { nameof(ToDoInfo.Description), nameof(ToDoInfo.Msg) },
            propertyNames);
    }
}
