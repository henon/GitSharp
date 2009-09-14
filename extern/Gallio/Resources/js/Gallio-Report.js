function reportLoaded()
{
    fixAttachmentLinksOnIE();
}

function fixAttachmentLinksOnIE()
{
    if (isIE() && (isLocalFileUri(window.location.href) || isInMemoryUri(window.location.href)))
    {
        // On IE, pages in the local filesystem that possess the Mark of the Web
        // are forbidden from navigating to other local files.  This breaks links
        // to attachments on the local filesystem unless we make some changes.
        var count = document.links.length;
        for (var i = 0; i < count; i++)
        {
            var link = document.links[i];
            var href = link.href;
            if (link.className == "attachmentLink" && isLocalFileUri(href))
            {
                var path = href.substring(8).replace(/\//g, "\\");
                link.href = "gallio:openAttachment?path=" + path;
            }
        }
    }
}

function isIE()
{
    return navigator.appName == "Microsoft Internet Explorer";
}

function isLocalFileUri(uri)
{
    return uri.search(/^file:\/\/\//) == 0;
}

function isInMemoryUri(uri)
{
    return uri == "about:blank";
}

function toggle(id)
{
    var icon = document.getElementById('toggle-' + id);
    if (icon != null)
    {
        var childElement = document.getElementById(id);
        if (icon.src.indexOf('Plus.gif') != -1)
        {
            icon.src = icon.src.replace('Plus.gif', 'Minus.gif');
            if (childElement != null)
                childElement.style.display = "block";
        }
        else
        {
            icon.src = icon.src.replace('Minus.gif', 'Plus.gif');
            if (childElement != null)
                childElement.style.display = "none";
        }
    }
}

function expand(ids)
{
    for (var i = 0; i < ids.length; i++)
    {
        var id = ids[i];
        var icon = document.getElementById('toggle-' + id);
        if (icon != null)
        {
            if (icon.src.indexOf('Plus.gif') != -1)
            {
                icon.src = icon.src.replace('Plus.gif', 'Minus.gif');

                var childElement = document.getElementById(id);
                if (childElement != null)
                    childElement.style.display = "block";
            }
        }
    }
}

function navigateTo(path, line, column)
{
    var navigator = new ActiveXObject("Gallio.Navigator.GallioNavigator");
    if (navigator)
        navigator.NavigateTo(path, line, column);
}

function setInnerHTMLFromUri(id, uri)
{
    _asyncLoadContentFromUri(uri, function(loadedDocument)
    {
        // workaround for IE failure to auto-detect HTML content
        var children = loadedDocument.body.children;
        if (children && children.length == 1 && children[0].tagName == "PRE")
        {
            var text = getTextContent(loadedDocument.body);
            setInnerHTMLFromContent(id, text);
        }
        else
        {
            var html = loadedDocument.body.innerHTML;
            setInnerHTMLFromContent(id, html);
        }
    });
}

function setInnerTextFromUri(id, uri)
{
    _asyncLoadContentFromUri(uri, function(loadedDocument) { setInnerTextFromContent(id, getTextContent(loadedDocument.body)); });
}

function setInnerHTMLFromHiddenData(id)
{
    var element = document.getElementById(id + '-hidden');
    if (element)
        setInnerHTMLFromContent(id, getTextContent(element));
}

function setInnerTextFromHiddenData(id)
{
    var element = document.getElementById(id + '-hidden');
    if (element)
        setInnerTextFromContent(id, getTextContent(element));
}

function setInnerHTMLFromContent(id, content)
{
    if (content != undefined)
    {
        var element = document.getElementById(id);
        if (element)
            element.innerHTML = content;
    }
}

function setInnerTextFromContent(id, content)
{
    if (content != undefined)
    {
        var element = document.getElementById(id);
        if (element)
            setTextContent(element, content);
    }
}

function getTextContent(element)
{
    return element.textContent != undefined ? element.textContent : element.innerText;
}

function setTextContent(element, content)
{
    if (element.textContent != undefined)
        element.textContent = content;
    else
        element.innerText = content;
}

function setFrameLocation(frame, uri)
{
    if (frame.contentWindow)
        frame.contentWindow.location.replace(uri);
}

function _asyncLoadContentFromUri(uri, callback)
{
    /* Removed due to race problems with IE during the initial page load.
    So instead we create the frame in the HTML.
    var asyncLoadFrame = document.getElementById('_asyncLoadFrame');
    if (!asyncLoadFrame)
    {
    asyncLoadFrame = document.createElement('iframe');
    asyncLoadFrame.setAttribute('id', '_asyncLoadFrame');
    asyncLoadFrame.style.border = '0px';
    asyncLoadFrame.style.width = '0px';
    asyncLoadFrame.style.height = '0px';
    asyncLoadFrame.style.display = 'none';
    document.body.appendChild(asyncLoadFrame);

        if (asyncLoadFrame.addEventListener)
    asyncLoadFrame.addEventListener('load', function(event) { _asyncLoadFrameOnLoad(asyncLoadFrame); }, false);
    else
    asyncLoadFrame.attachEvent('onload', function(event) { _asyncLoadFrameOnLoad(asyncLoadFrame); });

        asyncLoadFrame.pendingRequests = [];
    }
    */

    var asyncLoadFrame = document.getElementById('_asyncLoadFrame');

    if (!asyncLoadFrame.pendingRequests)
        asyncLoadFrame.pendingRequests = [];

    asyncLoadFrame.pendingRequests.push({ uri: uri, callback: callback });

    _asyncLoadFrameNext(asyncLoadFrame);
}

function _asyncLoadFrameOnLoad()
{
    var asyncLoadFrame = document.getElementById('_asyncLoadFrame');
    if (asyncLoadFrame)
    {
        var request = asyncLoadFrame.currentRequest;
        if (request)
        {
            asyncLoadFrame.currentRequest = undefined; // delete statement not supported by IE here

            try
            {
                var loadedWindow = asyncLoadFrame.contentWindow;
                if (loadedWindow && loadedWindow.location.href != "about:blank")
                {
                    var loadedDocument = loadedWindow.document;
                    if (loadedDocument)
                    {
                        request.callback(loadedDocument);
                    }
                }
            }
            catch (ex)
            {
                //alert(ex.message);
            }
        }

        _asyncLoadFrameNext(asyncLoadFrame);
    }
}

function _asyncLoadFrameNext(asyncLoadFrame)
{
    while (!asyncLoadFrame.currentRequest && asyncLoadFrame.pendingRequests && asyncLoadFrame.pendingRequests.length > 0)
    {
        var request = asyncLoadFrame.pendingRequests.shift();
        asyncLoadFrame.currentRequest = request;

        try
        {
            setFrameLocation(asyncLoadFrame, request.uri);
        }
        catch (ex)
        {
            //alert(ex.message);
        }
    }
}
