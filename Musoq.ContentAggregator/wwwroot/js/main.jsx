
class EditorMenu extends React.Component {
    constructor(props) {
        super(props);
    }

    getQueryName() {
        return this.refs.queryName.value;
    }

    componentDidMount() {
        return this.refs.queryName.value = this.props.fileName;
    }

    render() {
        return (
            <div className="menu">
                <input type="text" ref="queryName"/>
                <input type="submit" value="Save" onClick={this.props.onSave} />
                <input type="submit" value="Compile" onClick={this.props.onCompile} />
                <input type="submit" value="Run" onClick={this.props.onRun} />
            </div>);
    }
}

class CodeMirrorEditor extends React.Component {

    constructor(props) {
        super(props);
    }

    componentDidMount() {
        this.codeMirror = CodeMirror.fromTextArea(this.refs.textAreaNode);
        this.codeMirror.setValue(this.props.text);
    }

    getText() {
        return this.codeMirror.getValue();
    }

    render() {
        return (
            <div className="musoq">
                <textarea ref="textAreaNode" />
            </div>
        );
    }
}

class EditorContent extends React.Component {

    constructor(props) {
        super(props);
    }

    getEditorText() {
        return this.refs.editor.getText();
    }

    render() {
        return <CodeMirrorEditor ref="editor" text={this.props.text}/>
    }
}

class ResultContent extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            name: "",
            columns: [],
            rows: [[]],
            computationTime: 0
        };
    }

    updateResutSet(set) {
        this.setState(set);
    }

    render() {
        var header = [];
        for (var i = 0; i < this.state.columns.length; ++i) {
            header.push(<td>{this.state.columns[i]}</td>);
        }

        var rows = [];
        for (var i = 0; i < this.state.rows.length; ++i) {
            var columns = [];

            for (var j = 0; j < this.state.rows[i].length; ++j) {
                columns.push(<td>{this.state.rows[i][j]}</td>);
            }

            rows.push(<tr>{columns}</tr>);
        }

        return (
            <div>
                <div>{this.state.Name}</div>
                <button onClick={this.props.onClick}>x</button>
                
                <table>
                    <thead>
                        <tr>
                            {header}
                        </tr>
                    </thead>
                    <tbody>
                        {rows}
                    </tbody>
                </table>
            </div>
        );
    }
}

class Editor extends React.Component {

    constructor(props) {
        super(props);
        this.state = { showResults: false };

        if (this.props.initialData.text === null || this.props.initialData.text === undefined) {
            this.props.initialData.text = "";
        }

        if (this.props.initialData.name === null || this.props.initialData.name === undefined) {
            this.props.initialData.name = "";
        }
    }

    handleSave() {
        var modifiedQuery = {
            Text: this.refs.editorContent.getEditorText(),
            Name: this.refs.editorMenu.getQueryName(),
            ScriptId: this.props.initialData.scriptId
        };

        console.log(modifiedQuery);

        const data = new FormData();

        data.set('text', modifiedQuery.Text);
        data.set('name', modifiedQuery.Name);
        data.set('scriptId', modifiedQuery.ScriptId);

        fetch('/editor/save', {
            credentials: 'include',
            method: 'POST',
            body: data
        });
    }

    handleRun() {
        console.log('running');

        const data = new FormData();

        data.set('scriptId', this.props.initialData.scriptId);

        fetch('/editor/run', {
            credentials: 'include',
            method: 'POST',
            body: data
        }).then(resp => {
            resp.json().then((data) => {
                this.batchId = data.batchId;
                setTimeout(() => this.getTableScore(), 100);
            });
        });

        this.setState({ showResults: true });
    }

    getTableScore() {
        var result = this.checkHasScore();
    }

    checkHasScore() {
        const data = new FormData();

        data.set('batchId', this.batchId);

        var result = fetch('/editor/hasBatch', {
            credentials: 'include',
            method: 'POST',
            body: data
        });

        result.then((resp) => {
            resp.json().then(json => {
                if (json.hasBatch) {
                    fetch('/editor/table', {
                        credentials: 'include',
                        method: 'POST',
                        body: data
                    }).then(tr => {
                        tr.json().then(j => {
                            this.refs.resultContent.updateResutSet(j);
                        });
                    });
                }
                else {
                    setTimeout(() => this.getTableScore(), 100);
                }
            });
        });
        return true;
    }

    handleCompile() {
        console.log('compile');

        const data = new FormData();

        data.set('scriptId', this.props.initialData.scriptId);

        fetch('/editor/compile', {
            credentials: 'include',
            method: 'POST',
            body: data
        });
    }

    handleCloseResultWindow() {
        this.setState({ showResults: false });
    }

    setText(text) {
        this.refs.editorContent.setText(text);
    }

    render() {
        return (
            <div>
                <EditorMenu ref="editorMenu" fileName={this.props.initialData.name} onSave={() => this.handleSave()} onCompile={() => this.handleCompile()} onRun={() => this.handleRun()} />
                <EditorContent ref="editorContent" text={this.props.initialData.text} />
                {this.state.showResults ? <ResultContent ref="resultContent" onClick={() => this.handleCloseResultWindow()} /> : null}
            </div>
        );
    }
}