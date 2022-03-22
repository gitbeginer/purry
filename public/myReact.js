(function () {
    class Fnode {
        constructor() {
            this.dom = null;
            this.child = null;
            this.prop = null;
            this.type = null;
        }
    }
    function rmNode(node) {
        if (node.dom) node.dom.remove();
        if (!node.child) return;
        node.child.forEach(element => {
            rmNode(element)
        });
    }

    function fnodeLoad(node, ele, parent_dom) {
        if (typeof ele != 'object') {
            const type = '$' + ele;
            if (type == node.type) return;
            if (!node.dom) {
                node.dom = document.createTextNode('');
                parent_dom.appendChild(node.dom);
            }
            node.dom.textContent = ele;
            node.type = type;
            return;
        }

        if (ele.type != node.type) {
            node.type = ele.type;
            let newdom = null;
            if (typeof ele.type != 'function') {
                newdom = document.createElement(ele.type);
                if (node.dom) {
                    parent_dom.insertBefore(newdom, node.dom)
                } else {
                    parent_dom.appendChild(newdom);
                }
            }
            rmNode(node);
            node.dom = newdom;
            if (newdom) node.dom._node = node;
            node.child = [];
        }
        if (typeof ele.type == 'function') {
            ele.child.push(ele.type.call(null, ele.prop));
        } else {
            node.prop = node.prop || {};
            Object.keys(node.prop).filter(k => !ele.prop[k]).forEach(key => {
                node.dom.removeAttribute(key);
            });
            Object.keys(ele.prop).forEach(key => {
                if (node.prop[key] != ele.prop[key]) {
                    node.dom.setAttribute(key, ele.prop[key]);
                }
            });
            node.prop = ele.prop;
            parent_dom = node.dom;
        }
        if (!ele.child) return;
        for (var i = 0; i < ele.child.length; i++) {
            if (node.child.length <= i) node.child.push(new Fnode())
            fnodeLoad(node.child[i], ele.child[i], parent_dom)
        }
        node.child.slice(i).forEach(element => {
            rmNode(element)
        });
        node.child = node.child.slice(0, i)
    }

    const MyRT = {
        createElement: function (type, prop, child) {
            if (!type) throw new Error("type Error");
            prop = prop || {};
            if (typeof prop !== 'object') throw new Error("Attribute Error");
            if (arguments.length > 2) child = [...arguments].slice(2);
            child = child || []
            const element = { type, prop, child }
            return element;
        },
        render: function (ele, dom) {
            if (dom && typeof dom != 'object') throw new Error(dom + "is not an object");
            dom._rootNode = dom._rootNode || new Fnode();
            fnodeLoad(dom._rootNode, ele, dom)
        }
    }
    
    window.React = MyRT;

})();