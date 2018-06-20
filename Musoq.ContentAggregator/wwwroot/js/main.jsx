
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
            <div className="menu btn-toolbar mb-3" role="toolbar">
                <div class="btn-group mr-1" role="group" aria-label="First group">
                    <div class="input-group mb-3">
                        <div class="input-group-prepend">
                            <span class="input-group-text" id="basic-addon1">File Name</span>
                        </div>
                        <input type="text" ref="queryName" class="form-control" placeholder="Username" aria-label="Username" aria-describedby="basic-addon1" />
                    </div>
                </div>
                <div class="btn-group mr-1" role="group" aria-label="First group">
                    <button type="button" class="btn btn-secondary" onClick={this.props.onSave}>Save</button>
                    <button type="button" class="btn btn-secondary" onClick={this.props.onCompile}>Compile</button>
                    <button type="button" class="btn btn-secondary" onClick={this.props.onRun}>Run</button>
                </div>
                <div class="btn-group mr-1" role="group" aria-label="Second group">
                    <button type="button" class="btn btn-secondary" onClick={this.props.onExit}>Exit</button>
                </div>
            </div>);
    }
}

class CodeMirrorEditor extends React.Component {

    constructor(props) {
        super(props);
    }

    componentDidMount() {
        this.codeMirror = CodeMirror.fromTextArea(this.refs.textAreaNode, {
            lineNumbers: true
        });
        this.codeMirror.setValue(this.props.text);
        this.codeMirror.setSize("100%", "100%");
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
            <div class="table-feature">
                <div class="table-content">
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
        var modifiedQuery = this.getModifiedQuery();

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
        var modifiedQuery = this.getModifiedQuery();

        const data = new FormData();

        data.set('text', modifiedQuery.Text);
        data.set('name', modifiedQuery.Name);
        data.set('scriptId', modifiedQuery.ScriptId);

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

    handleExit() {
        window.location.href = "/Query/Index";
    }

    getModifiedQuery() {
        return {
            Text: this.refs.editorContent.getEditorText(),
            Name: this.refs.editorMenu.getQueryName(),
            ScriptId: this.props.initialData.scriptId
        };
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
                            console.log('updated');
                            this.props.onScoreAppeared(j);
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
        var modifiedQuery = this.getModifiedQuery();

        const data = new FormData();

        data.set('text', modifiedQuery.Text);
        data.set('name', modifiedQuery.Name);
        data.set('scriptId', modifiedQuery.ScriptId);

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
                <EditorMenu ref="editorMenu" fileName={this.props.initialData.name} onSave={() => this.handleSave()} onCompile={() => this.handleCompile()} onRun={() => this.handleRun()} onExit={() => this.handleExit()} />
                <EditorContent ref="editorContent" text={this.props.initialData.text} />
            </div>
        );
    }
}

class EditorWindow extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return (
            <div>
                <Editor ref="editor" initialData={this.props.initialData} onScoreAppeared={(set) => this.refs.results.updateResutSet(set)} />
                <ResultContent ref="results" />
            </div>);
    }
}