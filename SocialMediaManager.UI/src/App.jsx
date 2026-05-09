import React, { useState, useEffect } from 'react';
import { 
  Share2, CheckCircle, Loader2, Sparkles, Send, Calendar, 
  LayoutDashboard, PenSquare, X, Wand2, Image as ImageIcon, 
  Check, Lock, Mail, User, ArrowRight, LogOut, AlertCircle
} from 'lucide-react';
import axios from 'axios';

const getLocalISOString = () => {
  const tzoffset = (new Date()).getTimezoneOffset() * 60000;
  return (new Date(Date.now() - tzoffset)).toISOString().slice(0, 16);
};

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [authMode, setAuthMode] = useState('login'); 
  const [authLoading, setAuthLoading] = useState(false);
  const [authError, setAuthError] = useState('');

  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  
  const [currentUserName, setCurrentUserName] = useState('User');

  const [activeTab, setActiveTab] = useState('compose');
  const [postContent, setPostContent] = useState('');
  const [generatedText, setGeneratedText] = useState('');
  const [scheduledTime, setScheduledTime] = useState('');
  const [connections, setConnections] = useState({
    linkedIn: { isConnected: false, username: null },
    x: { isConnected: false, username: null }
  });
  const [posts, setPosts] = useState([]);
  const [isLoadingFeed, setIsLoadingFeed] = useState(false);
  const [selectedAccounts, setSelectedAccounts] = useState(['linkedin_mock_id']);
  const [postStatus, setPostStatus] = useState('idle');
  const [showAiAssistant, setShowAiAssistant] = useState(false);
  const [aiTopic, setAiTopic] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);

  const [selectedImage, setSelectedImage] = useState(null);
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = React.useRef(null);

  useEffect(() => {
    const token = localStorage.getItem('socialSyncToken');
    const savedName = localStorage.getItem('socialSyncUser');
    
    if (token) {
      axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      if (savedName) setCurrentUserName(savedName);
      setIsAuthenticated(true);
    }
  }, []);

  useEffect(() => {
    if (isAuthenticated) checkConnections();
  }, [isAuthenticated]);

  const handleAuthSubmit = async (e) => {
    e.preventDefault();
    setAuthLoading(true);
    setAuthError('');

    try {
      const endpoint = authMode === 'login' ? 'login' : 'register';
      const payload = {
        username: name || "User", 
        email: email,
        password: password
      };

      const response = await axios.post(`http://localhost:5195/api/auth/${endpoint}`, payload);
      const token = response.data.token;
      
      let realName = name || "User";
      try {
        const tokenData = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
        realName = tokenData.username || realName;
      } catch (e) {
        console.warn("Could not parse JWT for username");
      }
      
      localStorage.setItem('socialSyncToken', token);
      localStorage.setItem('socialSyncUser', realName);
      setCurrentUserName(realName);
      
      axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      setIsAuthenticated(true);
      setPassword('');
      setName('');
    } catch (error) {
      console.error(error);
      setAuthError(error.response?.data?.message || "An error occurred connecting to the server.");
      
      setPassword(''); 
    } finally {
      setAuthLoading(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('socialSyncToken');
    localStorage.removeItem('socialSyncUser');
    delete axios.defaults.headers.common['Authorization'];
    
    setIsAuthenticated(false);
    setActiveTab('compose');
    
    setEmail('');
    setPassword('');
    setName(''); 
    setAuthError('');
  };

  const handleAccountToggle = (id) => {
    setSelectedAccounts(prev => prev.includes(id) ? prev.filter(accId => accId !== id) : [...prev, id]);
  };

  const connectLinkedIn = () => {
    const clientId = "86ih02p9hq10ac";
    const redirectUri = encodeURIComponent("http://localhost:5173/oauth/callback/linkedin");
    const scope = encodeURIComponent("w_member_social openid profile email");
    window.location.href = `https://www.linkedin.com/oauth/v2/authorization?response_type=code&client_id=${clientId}&redirect_uri=${redirectUri}&state=linkedin_auth&scope=${scope}`;
  };

  const connectX = () => {
    const clientId = "YWZrY0lBWDNDb21naTZocFVEazE6MTpjaQ";
    const redirectUri = encodeURIComponent("http://localhost:5173/oauth/callback/x");
    const scope = encodeURIComponent("tweet.read tweet.write users.read offline.access");
    window.location.href = `https://twitter.com/i/oauth2/authorize?response_type=code&client_id=${clientId}&redirect_uri=${redirectUri}&scope=${scope}&state=x_auth&code_challenge=challenge_string&code_challenge_method=plain`;
  };

  const checkConnections = async () => {
    try {
      const response = await axios.get('http://localhost:5195/api/oauth/status');
      setConnections({
        linkedIn: response.data.linkedIn,
        x: response.data.x
      });
    } catch (e) {
      console.error("Failed to sync account status", e);
    }
  };

  const handleDisconnect = async (platform) => {
    try {
      await axios.delete(`http://localhost:5195/api/oauth/${platform}/disconnect`);
      checkConnections();
    } catch (e) {
      alert(`Failed to disconnect ${platform}`);
    }
  };

  const handleGenerateAI = async () => {
    if (!aiTopic) return;
    setIsGenerating(true);
    
    try {
      const response = await axios.post('http://localhost:5195/api/ai/generate', {
        topic: aiTopic
      });

      setPostContent(response.data.text);
      
      setShowAiAssistant(false);
      setAiTopic('');
    } catch (error) {
      console.error("AI Generation Error:", error);
      alert("Failed to generate AI content. Check backend console!");
    } finally {
      setIsGenerating(false);
    }
  };

  const handlePublish = async () => {
    if (selectedAccounts.length === 0) return alert("Please select an account!");
    setPostStatus('loading');
    setTimeout(() => {
      setPostStatus('success');
      setPostContent('');
      setTimeout(() => setPostStatus('idle'), 3000);
    }, 1500);
  };

  const handleImageUpload = async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    setIsUploading(true);
    const formData = new FormData();
    formData.append('file', file);

    try {
      const response = await axios.post('http://localhost:5195/api/media/upload', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      setSelectedImage(response.data.url);
    } catch (error) {
      const serverMessage = error.response?.data?.message || error.message;
      console.error("Upload failed details:", serverMessage);
      alert(`Upload failed: ${serverMessage}`);
    } finally {
      setIsUploading(false);
    }
  };

  const handleSavePost = async () => {
    if (!postContent) {
      alert("Please write or generate some text first!");
      return;
    }

    try {
      const payload = {
        content: postContent,
        imageUrl: selectedImage,
        scheduledFor: scheduledTime ? new Date(scheduledTime).toISOString() : null,
        selectedPlatforms: selectedAccounts.map(id => id === 'linkedin_mock_id' ? 'LinkedIn' : 'X')
      };

      const response = await axios.post('http://localhost:5195/api/post', payload);
      alert("Success! Your post has been scheduled.");
      
      setPostContent('');
      setSelectedImage(null);
      setScheduledTime('');
    } catch (error) {
      console.error("Failed to save post", error);
      alert("Failed to save post.");
    }
  };

  const fetchPosts = async () => {
    setIsLoadingFeed(true);
    try {
      const response = await axios.get('http://localhost:5195/api/post');
      setPosts(response.data);
    } catch (error) {
      console.error("Failed to fetch feed:", error);
    } finally {
      setIsLoadingFeed(false);
    }
  };

  useEffect(() => {
    let intervalId;
    
    if (activeTab === 'feed') {
      fetchPosts();

      intervalId = setInterval(() => {
        axios.get('http://localhost:5195/api/post')
          .then(res => setPosts(res.data))
          .catch(err => console.error("Polling error", err));
      }, 5000);
    }

    return () => clearInterval(intervalId);
  }, [activeTab]);

  const linkedAccounts = [
    { id: 'linkedin_mock_id', platform: 'LinkedIn', icon: 'in', color: 'bg-[#0077b5]' },
    { id: 'twitter_mock_id', platform: 'X (Twitter)', icon: 'X', color: 'bg-black' }
  ];

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen bg-slate-900 text-white font-sans flex items-center justify-center p-4 relative overflow-hidden">
        <div className="absolute top-[-10%] left-[-10%] w-96 h-96 bg-blue-600/20 rounded-full blur-3xl"></div>
        <div className="absolute bottom-[-10%] right-[-10%] w-96 h-96 bg-purple-600/20 rounded-full blur-3xl"></div>

        <div className="bg-slate-800/80 backdrop-blur-xl border border-slate-700 p-10 rounded-3xl shadow-2xl w-full max-w-md relative z-10 animate-in fade-in zoom-in-95 duration-500">
          
          <div className="flex justify-center items-center gap-2 text-3xl font-bold tracking-tight mb-2">
            <Share2 className="text-blue-500" size={36} />
            <span>Social<span className="text-blue-500">Sync</span></span>
          </div>
          <p className="text-center text-slate-400 mb-6 font-medium">
            {authMode === 'login' ? 'Welcome back to your workspace.' : 'Create your account to get started.'}
          </p>

          {authError && (
            <div className="mb-6 p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-400 text-sm flex items-center gap-2">
              <AlertCircle size={18} /> {authError}
            </div>
          )}

          <form onSubmit={handleAuthSubmit} className="flex flex-col gap-4">
            {authMode === 'signup' && (
              <div className="relative">
                <User className="absolute left-4 top-3.5 text-slate-500" size={20} />
                <input 
                  type="text" required placeholder="Full Name" 
                  value={name} onChange={(e) => setName(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-700 rounded-xl py-3 pl-12 pr-4 text-white focus:outline-none focus:border-blue-500 transition-all" 
                />
              </div>
            )}
            
            <div className="relative">
              <Mail className="absolute left-4 top-3.5 text-slate-500" size={20} />
              <input 
                type="email" required placeholder="Email Address" 
                value={email} onChange={(e) => setEmail(e.target.value)}
                className="w-full bg-slate-900 border border-slate-700 rounded-xl py-3 pl-12 pr-4 text-white focus:outline-none focus:border-blue-500 transition-all" 
              />
            </div>

            <div className="relative">
              <Lock className="absolute left-4 top-3.5 text-slate-500" size={20} />
              <input 
                type="password" required placeholder="Password" 
                value={password} onChange={(e) => setPassword(e.target.value)}
                className="w-full bg-slate-900 border border-slate-700 rounded-xl py-3 pl-12 pr-4 text-white focus:outline-none focus:border-blue-500 transition-all" 
              />
            </div>

            <button disabled={authLoading} type="submit" className="mt-4 flex items-center justify-center gap-2 w-full bg-blue-600 hover:bg-blue-500 text-white font-semibold py-3.5 rounded-xl transition-all disabled:opacity-50 shadow-lg shadow-blue-900/20">
              {authLoading ? <Loader2 className="animate-spin" size={20} /> : (
                <>
                  {authMode === 'login' ? 'Sign In' : 'Create Account'}
                  <ArrowRight size={20} />
                </>
              )}
            </button>
          </form>

          <div className="mt-6 text-center text-sm text-slate-400">
            {authMode === 'login' ? "Don't have an account? " : "Already have an account? "}
            <button 
              type="button"
              onClick={() => { 
                setAuthMode(authMode === 'login' ? 'signup' : 'login'); 
                setAuthError(''); 
                setEmail('');
                setPassword('');
                setName('');
              }}
              className="text-blue-400 hover:text-blue-300 font-semibold transition-colors"
            >
              {authMode === 'login' ? 'Sign up' : 'Log in'}
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-900 text-white font-sans p-8 flex gap-8">
      <aside className="w-64 shrink-0 flex flex-col justify-between h-[calc(100vh-4rem)] sticky top-8">
        <div>
          <div className="flex items-center gap-2 text-2xl font-bold tracking-tight px-4 mb-8">
            <Share2 className="text-blue-500" size={32} />
            <span>Social<span className="text-blue-500">Sync</span></span>
          </div>
          
          <nav className="flex flex-col gap-2">
            <button onClick={() => setActiveTab('accounts')} className={`flex items-center gap-3 px-4 py-3 rounded-xl font-medium transition-all ${activeTab === 'accounts' ? 'bg-blue-600' : 'text-slate-400 hover:bg-slate-800'}`}>
              <LayoutDashboard size={20} /> Manage Accounts
            </button>
            <button onClick={() => setActiveTab('compose')} className={`flex items-center gap-3 px-4 py-3 rounded-xl font-medium transition-all ${activeTab === 'compose' ? 'bg-blue-600' : 'text-slate-400 hover:bg-slate-800'}`}>
              <PenSquare size={20} /> Create Post
            </button>
            <button 
              onClick={() => { setActiveTab('feed'); fetchPosts(); }} 
              className={`flex items-center gap-3 px-4 py-3 rounded-xl font-medium transition-all ${activeTab === 'feed' ? 'bg-blue-600' : 'text-slate-400 hover:bg-slate-800'}`}
            >
              <LayoutDashboard size={20} /> My Posts
            </button>
          </nav>
        </div>

        <div className="px-4 border-t border-slate-800 pt-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-slate-800 rounded-full flex items-center justify-center font-bold text-blue-400 uppercase tracking-wider">
                {currentUserName.substring(0, 2)}
              </div>
              <div>
                <p className="text-sm font-semibold truncate w-24">{currentUserName}</p>
                <p className="text-xs text-slate-500">Pro Plan</p>
              </div>
            </div>
            <button onClick={handleLogout} className="text-slate-500 hover:text-red-400 transition-colors" title="Logout">
              <LogOut size={18} />
            </button>
          </div>
        </div>
      </aside>

      <main className="flex-1 max-w-3xl">
        {activeTab === 'accounts' && (
          <div className="bg-slate-800 border border-slate-700 rounded-3xl p-8 shadow-2xl animate-in fade-in duration-500">
            <h2 className="text-2xl font-semibold mb-2">Connect Channels</h2>
            <p className="text-slate-400 mb-8">Link your accounts to start scheduling posts.</p>
            {/* LinkedIn Connection Row */}
            <div className="flex items-center justify-between p-5 bg-slate-900/50 rounded-2xl border border-slate-700/50">
              <div className="flex items-center gap-4">
                <div className="w-12 h-12 bg-[#0077b5] rounded-xl flex items-center justify-center text-xl font-bold">in</div>
                <div>
                  <h3 className="font-medium text-lg">LinkedIn</h3>
                  {connections.linkedIn.isConnected ? (
                    <p className="text-sm text-green-400 font-medium">Connected as {connections.linkedIn.username || "User"}</p>
                  ) : (
                    <p className="text-xs text-slate-500 uppercase tracking-widest font-bold">Professional Network</p>
                  )}
                </div>
              </div>
              {connections.linkedIn.isConnected ? (
                <button onClick={() => handleDisconnect('linkedin')} className="px-6 py-2.5 rounded-xl font-semibold bg-red-500/10 text-red-500 hover:bg-red-500/20 transition-all">
                  Disconnect
                </button>
              ) : (
                <button onClick={connectLinkedIn} className="bg-blue-600 hover:bg-blue-500 px-6 py-2.5 rounded-xl font-semibold transition-all">
                  Connect
                </button>
              )}
            </div>

            {/* X (Twitter) Connection Row */}
            <div className="flex items-center justify-between p-5 bg-slate-900/50 rounded-2xl border border-slate-700/50 mt-4">
              <div className="flex items-center gap-4">
                <div className="w-12 h-12 bg-black rounded-xl flex items-center justify-center text-xl font-bold text-white">X</div>
                <div>
                  <h3 className="font-medium text-lg">X (Twitter)</h3>
                  {connections.x.isConnected ? (
                    <p className="text-sm text-green-400 font-medium">Connected as {connections.x.username || "User"}</p>
                  ) : (
                    <p className="text-xs text-slate-500 uppercase tracking-widest font-bold">Social Microblogging</p>
                  )}
                </div>
              </div>
              {connections.x.isConnected ? (
                <button onClick={() => handleDisconnect('x')} className="px-6 py-2.5 rounded-xl font-semibold bg-red-500/10 text-red-500 hover:bg-red-500/20 transition-all">
                  Disconnect
                </button>
              ) : (
                <button onClick={connectX} className="bg-blue-600 hover:bg-blue-500 px-6 py-2.5 rounded-xl font-semibold transition-all">
                  Connect
                </button>
              )}
            </div>
          </div>
        )}

        {/* TAB 2: COMPOSE POST */}
        {activeTab === 'compose' && (
          <div className="bg-slate-800 border border-slate-700 rounded-3xl p-8 shadow-2xl animate-in fade-in duration-500">
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-2xl font-semibold">Create Post</h2>
              {!showAiAssistant && (
                <button onClick={() => setShowAiAssistant(true)} className="text-sm flex items-center gap-2 text-purple-400 bg-purple-400/10 hover:bg-purple-400/20 px-4 py-2 rounded-full transition-all border border-purple-400/20 font-medium">
                  <Sparkles size={16} /> Generate Text
                </button>
              )}
            </div>

            {showAiAssistant && (
              <div className="mb-6 p-5 bg-purple-900/10 border border-purple-500/30 rounded-2xl relative">
                <button onClick={() => setShowAiAssistant(false)} className="absolute top-4 right-4 text-purple-400"><X size={20} /></button>
                <h3 className="font-medium text-purple-300 flex items-center gap-2 mb-3"><Sparkles size={18} /> AI Generator</h3>
                <div className="flex gap-3">
                  <input type="text" value={aiTopic} onChange={(e) => setAiTopic(e.target.value)} placeholder="What should the post be about?" className="flex-1 bg-slate-900/50 border border-purple-500/30 rounded-xl p-3 text-white focus:outline-none focus:border-purple-500 transition-all" />
                  <button onClick={handleGenerateAI} disabled={!aiTopic || isGenerating} className="flex items-center gap-2 px-6 py-3 rounded-xl font-semibold bg-purple-600 hover:bg-purple-500 transition-all disabled:opacity-50">
                    {isGenerating ? <Loader2 className="animate-spin" size={18} /> : <Wand2 size={18} />} Generate
                  </button>
                </div>
              </div>
            )}
            
            <div className="mb-4 relative">
              <textarea 
                rows="6" placeholder="What do you want to share today?" value={postContent} onChange={(e) => setPostContent(e.target.value)}
                className="w-full bg-slate-900 border border-slate-700 rounded-2xl p-5 pb-14 text-white focus:outline-none focus:border-blue-500 resize-none transition-all text-lg"
              />
              <div className="absolute bottom-4 left-4 flex items-center gap-3">
                <input 
                  type="file" 
                  hidden 
                  ref={fileInputRef} 
                  onChange={handleImageUpload} 
                  accept="image/*" 
                />
                
                <button 
                  type="button"
                  onClick={() => fileInputRef.current.click()}
                  disabled={isUploading}
                  className="flex items-center gap-2 text-slate-400 hover:text-blue-400 transition-colors bg-slate-800 px-3 py-1.5 rounded-lg border border-slate-700 text-sm font-medium"
                >
                  {isUploading ? <Loader2 className="animate-spin" size={18} /> : <ImageIcon size={18} />}
                  {selectedImage ? "Change Image" : "Add Image"}
                </button>

                {selectedImage && (
                  <div className="relative w-10 h-10 rounded-lg overflow-hidden border border-slate-600">
                    <img src={selectedImage} alt="Preview" className="w-full h-full object-cover" />
                    <button 
                      onClick={() => setSelectedImage(null)}
                      className="absolute top-0 right-0 bg-red-500 p-0.5 rounded-bl-lg"
                    >
                      <X size={10} />
                    </button>
                  </div>
                )}
              </div>
            </div>

            <div className="mb-8">
              <p className="text-sm font-medium text-slate-400 mb-3">Post to:</p>
              <div className="flex gap-3">
                {linkedAccounts.map(acc => {
                  const isSelected = selectedAccounts.includes(acc.id);
                  return (
                    <button key={acc.id} onClick={() => handleAccountToggle(acc.id)} className={`flex items-center gap-2 px-4 py-2 rounded-xl border transition-all ${isSelected ? 'bg-slate-700 border-blue-500 ring-1 ring-blue-500' : 'bg-slate-900 border-slate-700 hover:border-slate-500 opacity-60'}`}>
                      <div className={`w-5 h-5 ${acc.color} rounded flex items-center justify-center text-[10px] font-bold`}>{acc.icon}</div>
                      <span className="font-medium text-sm">{acc.platform}</span>
                      {isSelected && <Check size={14} className="text-blue-400 ml-1" />}
                    </button>
                  );
                })}
              </div>
            </div>

            <div className="flex items-center justify-end border-t border-slate-700 pt-6">
              <div className="flex items-center gap-4">
                
                <div className="flex items-center gap-2 bg-slate-900 border border-slate-700 rounded-lg px-3 py-1.5 focus-within:border-blue-500 transition-colors">
                  <Calendar size={16} className="text-slate-400" />
                  <input 
                    type="datetime-local" 
                    value={scheduledTime}
                    onChange={(e) => setScheduledTime(e.target.value)}
                    className="bg-transparent text-slate-300 text-sm focus:outline-none"
                  />
                </div>

                <button 
                  onClick={handleSavePost}
                  className="bg-blue-600 hover:bg-blue-700 text-white px-5 py-2.5 rounded-xl font-semibold transition-all shadow-lg shadow-blue-600/20"
                >
                  {scheduledTime ? "Schedule Post" : "Post Now"}
                </button>
              </div>
            </div>

            {postStatus === 'success' && (
              <div className="mt-6 p-4 bg-green-500/10 border border-green-500/20 rounded-xl text-green-400 text-sm flex items-center gap-2 animate-in fade-in">
                <CheckCircle size={18} /> Posted successfully!
              </div>
            )}
          </div>
        )}

        {activeTab === 'feed' && (
          <div className="bg-slate-800 border border-slate-700 rounded-3xl p-8 shadow-2xl animate-in fade-in duration-500">
            <h2 className="text-2xl font-semibold mb-6">Scheduled Posts</h2>
            
            {isLoadingFeed ? (
              <div className="flex justify-center py-10"><Loader2 className="animate-spin text-blue-500" size={32} /></div>
            ) : posts.length === 0 ? (
              <div className="text-center text-slate-400 py-10 border border-slate-700 border-dashed rounded-2xl">
                No posts yet. Go create one!
              </div>
            ) : (
              <div className="flex flex-col gap-6">
                {posts.map(post => (
                  <div key={post.id} className="bg-slate-900 border border-slate-700 rounded-2xl p-5 shadow-lg">
                    
                    <div className="flex justify-between items-center mb-4">
                      <div className="flex items-center gap-2 text-sm text-slate-400">
                        <Calendar size={14} /> Scheduled for {new Date(post.scheduledFor).toLocaleString()}
                      </div>
                      <div className="flex gap-3 mt-2">
                        {post.platforms && post.platforms.map(plat => (
                          <div key={plat.id} className="flex items-center gap-2 bg-slate-900/50 px-3 py-1.5 rounded-lg border border-slate-700">
                            <div className={`w-5 h-5 rounded flex items-center justify-center text-[10px] font-bold ${plat.platformName === 'LinkedIn' ? 'bg-[#0077b5]' : 'bg-black'}`}>
                              {plat.platformName === 'LinkedIn' ? 'in' : 'X'}
                            </div>

                            {plat.isPublished ? (
                              <span className="text-xs text-green-500 font-bold flex items-center gap-1">
                                <CheckCircle size={12} /> Published
                              </span>
                            ) : (
                              <span className="text-xs text-yellow-500 font-bold flex items-center gap-1">
                                <Loader2 size={12} className="animate-spin" /> Pending
                              </span>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>

                    <p className="text-slate-200 whitespace-pre-wrap mb-4 text-lg">
                      {post.content}
                    </p>

                    {post.imageUrl && (
                      <div className="rounded-xl overflow-hidden border border-slate-700 bg-black max-h-96 flex items-center justify-center">
                        <img src={post.imageUrl} alt="Post media" className="object-cover max-h-96 w-full" />
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  );
}

export default App;