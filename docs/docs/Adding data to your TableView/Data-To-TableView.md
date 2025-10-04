# Adding data to your TableView
There are three different ways to add columns to your TableView, each one with their own use case. Before you get started, you'll need to find out which method you are going to use to add the columns.

---

### 1. Adding columns directly using XAML
<span style="color:red;">⚠️ This approach is not recommended since it still needs code-behind implementation and it can be tricky to set up Binding for XAML. You can just use Method 2 for this.</span>

<kbd>👉 [**View example in README**](..\getting-started.md)</kbd>

---

### 2. Assigning columns from a collection
This approach lets you create a TableView where the columns are **predetermined**. New rows can be added dynamically. Perfect for simple setups where you have a predetermined amount of columns, like a contact list (where there are columns name, phone number, email) where the user can add a new contact on-demand, or a static list like an ingredients list for a recipe.

<kbd>[**👉 Go to documentation**](Assigning-Columns-From-A-Collection.md)</kbd>

---

### 3. Dynamically creating columns using `DataTemplate`.
More advanced option that allows you to add **new** columns dynamically, while having the same functionality as option 2. Good choice for things like custom lists, where the user can add their own fields and columns.

<kbd>[**👉 Go to documentation**](Dynamically-Creating-Columns-Using-Template.md)</kbd>

---

##### Need help? <kbd>[**Go to Discussions**](https://github.com/w-ahmad/WinUI.TableView/discussions)</kbd>