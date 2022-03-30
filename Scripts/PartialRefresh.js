function installPartialRefresh(serviceURL, divContainerId, refreshRate, callBack = null) {
    // intallation of partial refresh
    setInterval(() => { DoPartialRefresh(serviceURL, divContainerId, callBack); }, refreshRate * 1000);
}

function DoPartialRefresh(serviceURL, divContainerId, callBack = null) {
    // posts partial refresh
    $.ajax({
        url: serviceURL,
        dataType: "html",
        success: function (htmlContent) {
            if (htmlContent !== "") {
                $("#" + divContainerId).html(htmlContent);
                if (callBack != null) callBack();
            }
        }
    })
}

function ajaxActionCall(actionLink) {
    // Ajax Action Call to actionLink
    $.ajax({
        url: actionLink,
        method: 'GET',
        success: (data) => {
            console.log("Result: " + data);
        }
    });
}

