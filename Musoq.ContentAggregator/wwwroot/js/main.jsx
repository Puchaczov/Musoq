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
    }

    render() {
        return (
            <div>
                <div>Tabelka</div>
                <button onClick={this.props.onClick}>x</button>
                <table>
                    <thead></thead>
                    <tbody></tbody>
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
        });

        this.setState({ showResults: true });
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