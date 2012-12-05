var AIS = {
    AzureTicker: {}
};

var service = "ServiceLocation";

String.prototype.trim = function () {
    return this.replace(/^\s+|\s+$/g, "");
}

AIS.AzureTicker.notifier = function () {
        return {
            submitRegistration: function (userName, password) {
                document.getElementById("ProgressContainer").style.display = "block";
                if (userName.trim() != "" && password.trim() != "") {
                    var channel = new Windows.Networking.PushNotifications.PushNotificationChannelManager.createPushNotificationChannelForApplicationAsync()
                        .done(
                        function (context) {
                            var localSettings = Windows.Storage.ApplicationData.current.localSettings;
                            var value = localSettings.values["notificationKey"];

                            var notification = {
                                RowKey: value,
                                userName: userName,
                                password: password,
                                notificationUri: context.uri
                            };

                            WinJS.xhr(
                                {
                                    type: 'POST',
                                    url: service,
                                    headers: { 'Content-Type': 'application/json; charset=utf-8' },
                                    data: JSON.stringify(notification)
                                }).done(
                                function onComplete(result) {
                                    var localSettings = Windows.Storage.ApplicationData.current.localSettings;
                                    localSettings.values["notificationKey"] = result.responseText.replace(/\"/g, "");
                                    var messageDialog = new Windows.UI.Popups.MessageDialog("Your settings have been updated. If the credentials are valid, you will receive an updated account balance shortly.");
                                    messageDialog.showAsync();
                                    document.getElementById("RegisteredUser").innerText = "Currently registered as: " + userName;
                                    document.getElementById("ProgressContainer").style.display = "none";
                                    document.getElementById("btnRemoveRegistration").style.visibility = "visible";
                                    document.getElementById("btnSubmit").value = "Update Registration";
                                    document.getElementById("txtPassword").placeholder = "Password required to update registration...";
                                    document.getElementById("txtPassword").value = "";
                                },
                                function onError(err) {
                                    var messageDialog = new Windows.UI.Popups.MessageDialog("There was a problem updating your settings.");
                                    messageDialog.showAsync();
                                    document.getElementById("ProgressContainer").style.display = "none";
                                },
                                function inProgress(context) {

                                });
                        },
                        function (context) {
                        },
                        function (context) {
                        }
                        );
                }
                else {
                    document.getElementById("ProgressContainer").style.display = "none";
                    var messageDialog = new Windows.UI.Popups.MessageDialog("Invalid Credentials.");
                    messageDialog.showAsync();
                }
            }
        };
        
}();

AIS.AzureTicker.removal = function () {
    return {
        removeRegistration: function () {
            document.getElementById("ProgressContainer").style.display = "block";
            var localSettings = Windows.Storage.ApplicationData.current.localSettings;
            var value = localSettings.values["notificationKey"];
            if (value) {
                WinJS.xhr(
                    {
                        type: 'POST',
                        url: service + 'RemoveExistingAccount?rowKey=' + value,
                        headers: { 'Content-Type': 'application/json; charset=utf-8' },
                    }).done(
                    function onComplete(result) {
                        var localSettings = Windows.Storage.ApplicationData.current.localSettings;
                        localSettings.values["notificationKey"] = null;
                        var messageDialog = new Windows.UI.Popups.MessageDialog("Your account has been removed. You will no longer receive updates until you re-register..");
                        messageDialog.showAsync();
                        document.getElementById("RegisteredUser").innerText = "Please register by entering your Windows Azure account credentials below.";
                        document.getElementById("ProgressContainer").style.display = "none";
                        document.getElementById("btnRemoveRegistration").style.visibility = "hidden";
                        document.getElementById("btnSubmit").value = "Submit Registration";
                        document.getElementById("txtPassword").placeholder = "Password required for registration...";
                        document.getElementById("txtPassword").value = "";
                        document.getElementById("txtUserName").value = "";
                    },
                    function onError(err) {
                        var messageDialog = new Windows.UI.Popups.MessageDialog("There was a problem removing your account. Please try again.");
                        messageDialog.showAsync();
                        document.getElementById("ProgressContainer").style.display = "none";
                        document.getElementById("btnRemoveRegistration").style.visibility = "visible";
                    },
                    function inProgress(context) {
                    });
            }
            else {
                document.getElementById("ProgressContainer").style.display = "none";
            }
        }
    };
}();

AIS.AzureTicker.loader = function () {

    return {
        showRegisteredData: function () {
            document.getElementById("ProgressContainer").style.display = "block";
            var localSettings = Windows.Storage.ApplicationData.current.localSettings;
            var value = localSettings.values["notificationKey"];
            if (value) {
                var channel = new Windows.Networking.PushNotifications.PushNotificationChannelManager.createPushNotificationChannelForApplicationAsync()
                    .done(
                    function (context) {
                        var appId = context.uri;

                        WinJS.xhr(
                            {
                                type: 'GET',
                                url: service + 'GetExistingUsername?rowKey=' + value + '&notificationUri=' + appId,
                                headers: { 'Content-Type': 'application/json; charset=utf-8' },
                            }).done(
                            function onComplete(result) {
                                if (result.responseText != "\"\"") {
                                    document.getElementById("ProgressContainer").style.display = "none";
                                    document.getElementById("RegisteredUser").innerText = "Currently registered as: " + result.responseText.replace(/\"/g, "");
                                    document.getElementById("btnRemoveRegistration").style.visibility = "visible";
                                    document.getElementById("btnSubmit").value = "Update Registration";
                                    document.getElementById("txtPassword").placeholder = "Password required to update registration...";
                                    document.getElementById("txtUserName").value = result.responseText.replace(/\"/g, "");
                                }
                                else {
                                    document.getElementById("ProgressContainer").style.display = "none";
                                    document.getElementById("RegisteredUser").innerText = "Please register by entering your Windows Azure account credentials below.";
                                    document.getElementById("btnRemoveRegistration").style.visibility = "hidden";
                                    document.getElementById("btnSubmit").value = "Submit Registration";
                                }
                            },
                            function onError(err) {
                                document.getElementById("ProgressContainer").style.display = "none";
                            }
                            );
                    }
                );
            }
            else {
                document.getElementById("ProgressContainer").style.display = "none";
            }
        }
    };
}();

