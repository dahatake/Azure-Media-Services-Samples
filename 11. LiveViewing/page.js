$(function() {
    var client = new WindowsAzure.MobileServiceClient('https://mskktelemonthlive.azure-mobile.net/', '<Mobile App‚ÌƒL[>'),
        todoItemTable = client.getTable('todoitem');

    // Read current data and rebuild UI.
    // If you plan to generate complex UIs like this, consider using a JavaScript templating library.
    function refreshTodoItems() {
//        var query = todoItemTable.where({ complete: false });
        var query = todoItemTable.orderByDescending("__createdAt");

        query.read().then(function(todoItems) {
            var listItems = $.map(todoItems, function(item) {
                return $('<li>')
                    .attr('data-todoitem-id', item.id)
                    .append($('<div>').append($('<textarea class="item-text" readonly="readonly" rows="3" wrap="soft">').val(item.text)));
            });

            $('#todo-items').empty().append(listItems).toggle(listItems.length > 0);
            $('#summary').html('<strong>' + todoItems.length + '</strong> comments');
        }, handleError);
    }

    function handleError(error) {
        var text = error + (error.request ? ' - ' + error.request.status : '');
        $('#errorlog').append($('<li>').text(text));
    }

    function getTodoItemId(formElement) {
        return $(formElement).closest('li').attr('data-todoitem-id');
    }

    // Handle insert
    $('#add-item').submit(function (evt) {

        var textbox = $('#new-item-text');

        if  (textbox.val() !== '') {
	        var dt = new Date();
	        var dtyear = dt.getFullYear();
	        var dtmonth = dt.getMonth() + 1;
	        if (dtmonth < 10) {
	            dtmonth = "0" + dtmonth;
	        }
	        var dtdate = dt.getDate();
	        if (dtdate < 10) {
	            dtdate = "0" + dtdate;
	        }
	        var dthour = dt.getHours();
	        if (dthour < 10) {
	            dthour = "0" + dthour;
	        }
	        var dtmin = dt.getMinutes();
	        if (dtmin < 10) {
	            dtmin = "0" + dtmin;
	        }
	        var dtsec = dt.getSeconds();
	        if (dtsec < 10) {
	            dtsec = "0" + dtsec;
	        }

	        var dtstr = dtyear + "/";
	        dtstr += dtmonth + "/";
	        dtstr += dtdate + " ";
	        dtstr += dthour + ":";
	        dtstr += dtmin + ":";
	        dtstr += dtsec;

			var itemText = "[" + dtstr + "] " + textbox.val();

            //if ((textbox.val() !== '') && (textbox2.val() !== '')) {
            todoItemTable.insert({ text: itemText, complete: false }).then(refreshTodoItems, handleError);
        }
        textbox.val('').focus();
        evt.preventDefault();
    });

    // Handle update
    $(document.body).on('change', '.item-text', function() {
        var newText = $(this).val();
        todoItemTable.update({ id: getTodoItemId(this), text: newText }).then(null, handleError);
    });

    $(document.body).on('change', '.item-complete', function() {
        var isComplete = $(this).prop('checked');
        todoItemTable.update({ id: getTodoItemId(this), complete: isComplete }).then(refreshTodoItems, handleError);
    });

    // Handle delete
    $(document.body).on('click', '.item-delete', function () {
        todoItemTable.del({ id: getTodoItemId(this) }).then(refreshTodoItems, handleError);
    });

    // On initial load, start by fetching the current data
    refreshTodoItems();
});